import json
import socket
import threading
import time
from typing import Dict, List, Optional, Tuple


# Unity 側 GuiCommandTcpClientSender の Host / Port と合わせる。
BIND_HOST = "0.0.0.0"
PORT = 5000


class MockBackend:
    def __init__(self, host: str, port: int):
        self.host = host
        self.port = port

        self.running = True
        self.server_socket: Optional[socket.socket] = None

        self.clients: List[Tuple[socket.socket, Tuple[str, int]]] = []
        self.clients_lock = threading.Lock()

        # Unity 側 Event parser がまだ message_type 前提なら message_type。
        # backend_ver019 系に合わせるなら type。
        self.event_field_name = "message_type"

        # 手動確認用。自動 status 送信は初期 OFF。
        self.auto_mecha_status = False
        self.auto_half_delay_sec = 1.0
        self.auto_full_delay_sec = 1.0
        self.auto_close_delay_sec = 1.0

    def start(self):
        server_thread = threading.Thread(target=self._run_server, daemon=True)
        server_thread.start()

        print("[MockBackend] Started.")
        print(f"[MockBackend] Listening target: {self.host}:{self.port}")
        print("[MockBackend] This app receives Unity Commands and sends Events to Unity.")
        self._print_help()

        self._run_console_loop()

    def _run_server(self):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
            self.server_socket = server_socket
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind((self.host, self.port))
            server_socket.listen(8)

            print(f"[MockBackend] Listening on {self.host}:{self.port}")

            while self.running:
                try:
                    client_socket, address = server_socket.accept()
                    client_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

                    with self.clients_lock:
                        self.clients.append((client_socket, address))

                    print(f"[MockBackend] Unity connected: {address}")

                    client_thread = threading.Thread(
                        target=self._run_client_reader,
                        args=(client_socket, address),
                        daemon=True,
                    )
                    client_thread.start()

                except OSError:
                    break

    def _run_client_reader(self, client_socket: socket.socket, address: Tuple[str, int]):
        buffer = ""

        try:
            while self.running:
                data = client_socket.recv(4096)

                if not data:
                    break

                text = data.decode("utf-8", errors="replace")
                buffer += text

                while "\n" in buffer:
                    line, buffer = buffer.split("\n", 1)
                    line = line.strip()

                    if not line:
                        continue

                    self._handle_received_line(line, address)

        except OSError as error:
            print(f"[MockBackend] Client read error {address}: {error}")

        finally:
            self._remove_client(client_socket, address)

    def _handle_received_line(self, line: str, address: Tuple[str, int]):
        print(f"[MockBackend] Received from Unity {address}: {line}")

        try:
            message = json.loads(line)
        except json.JSONDecodeError as error:
            print(f"[MockBackend] Received invalid JSON: {error}")
            return

        message_type = self._get_message_type(message)
        payload = message.get("payload", {})

        print(f"[MockBackend] Command type={message_type} payload={payload}")

        if self.auto_mecha_status:
            self._auto_reply_to_mecha_command(message_type)

    def _auto_reply_to_mecha_command(self, message_type: str):
        if message_type == "half_mode_cmd":
            self._send_status_after_delay("half_mode_sts", self.auto_half_delay_sec)
            return

        if message_type == "full_mode_cmd":
            self._send_status_after_delay("full_mode_sts", self.auto_full_delay_sec)
            return

        if message_type == "close_mode_cmd":
            self._send_status_after_delay("close_mode_sts", self.auto_close_delay_sec)

    def _send_status_after_delay(self, status_type: str, delay_sec: float):
        def worker():
            time.sleep(delay_sec)
            self.send_event(status_type, {})

        thread = threading.Thread(target=worker, daemon=True)
        thread.start()

    def _run_console_loop(self):
        while self.running:
            try:
                command = input("> ").strip()
            except EOFError:
                break
            except KeyboardInterrupt:
                self.running = False
                self._close_all_clients()
                self._close_server_socket()
                break

            if not command:
                continue

            if command == "q":
                self.running = False
                self._close_all_clients()
                self._close_server_socket()
                break

            self._handle_console_command(command)

    def _handle_console_command(self, command: str):
        parts = command.split()

        if command == "help":
            self._print_help()
            return

        if command == "clients":
            self._print_clients()
            return

        if command == "format":
            print(f"[MockBackend] Current event field name: {self.event_field_name}")
            return

        if command == "format message_type":
            self.event_field_name = "message_type"
            print("[MockBackend] Event field name = message_type")
            return

        if command == "format type":
            self.event_field_name = "type"
            print("[MockBackend] Event field name = type")
            return

        if command == "auto":
            print(f"[MockBackend] auto_mecha_status = {self.auto_mecha_status}")
            return

        if command == "auto on":
            self.auto_mecha_status = True
            print("[MockBackend] auto_mecha_status = true")
            return

        if command == "auto off":
            self.auto_mecha_status = False
            print("[MockBackend] auto_mecha_status = false")
            return

        if command == "ig_on":
            self.send_event("EVT_IG_ON", {})
            return

        if command == "ig_off":
            self.send_event("EVT_IG_OFF", {})
            return

        if command == "ig_on_short":
            self.send_event("IG_ON", {})
            return

        if command == "ig_off_short":
            self.send_event("IG_OFF", {})
            return

        if command == "p":
            self.send_event("EVT_SHIFTER_CHANGED", {"gear": "parking"})
            return

        if command == "d":
            self.send_event("EVT_SHIFTER_CHANGED", {"gear": "drive"})
            return

        if command == "r":
            self.send_event("EVT_SHIFTER_CHANGED", {"gear": "reverse"})
            return

        if command == "hvac":
            self.send_event("EVT_HVAC_POPUP", {})
            return

        if command == "volup":
            self.send_event("EVT_MEDIA_VOLUME_UP", {})
            return

        if command == "voldown":
            self.send_event("EVT_MEDIA_VOLUME_DOWN", {})
            return

        if command == "soundup":
            self.send_event("SIG_SOUND_VOLUME_UP", {})
            return

        if command == "sounddown":
            self.send_event("SIG_SOUND_VOLUME_DOWN", {})
            return

        if command == "led_color":
            self.send_event("SIG_LED_SUB_TOGGLE_COLOR", {})
            return

        if command == "led_pattern":
            self.send_event("SIG_LED_SUB_TOGGLE_PATTERN", {})
            return

        if command == "half_sts":
            self.send_event("half_mode_sts", {})
            return

        if command == "full_sts":
            self.send_event("full_mode_sts", {})
            return

        if command == "close_sts":
            self.send_event("close_mode_sts", {})
            return

        if command == "other_sts":
            self.send_event("other_mode_sts", {})
            return

        if command == "touchd":
            self._send_tap(115, 300, "driver")
            return

        if command == "touchp":
            self._send_tap(1325, 360, "passenger")
            return

        if command == "dragd":
            self._send_drag(650, 950, 1000, 950, "driver")
            return

        if command == "dragp":
            self._send_drag(1800, 360, 800, 360, "passenger")
            return

        if len(parts) == 4 and parts[0] in ("td", "tm", "tu"):
            self._handle_single_touch_command(parts)
            return

        if len(parts) == 4 and parts[0] == "tap":
            self._handle_tap_command(parts)
            return

        if len(parts) == 6 and parts[0] == "drag":
            self._handle_drag_command(parts)
            return

        if len(parts) >= 2 and parts[0] == "send":
            self._handle_raw_send_command(command)
            return

        print("[MockBackend] Unknown command. Type 'help'.")

    def _handle_raw_send_command(self, command: str):
        raw_json = command[len("send"):].strip()

        if not raw_json:
            print("[MockBackend] Usage: send {\"message_type\":\"EVT_IG_ON\",\"payload\":{}}")
            return

        try:
            json.loads(raw_json)
        except json.JSONDecodeError as error:
            print(f"[MockBackend] Invalid JSON: {error}")
            return

        self._send_line(raw_json)

    def _handle_single_touch_command(self, parts: List[str]):
        key = parts[0]
        x_text = parts[1]
        y_text = parts[2]
        source = parts[3]

        event_map = {
            "td": "down",
            "tm": "move",
            "tu": "up",
        }

        if source not in ("driver", "passenger"):
            print("[MockBackend] source must be driver or passenger.")
            return

        try:
            x = int(x_text)
            y = int(y_text)
        except ValueError:
            print("[MockBackend] x/y must be int.")
            return

        self.send_touch(x, y, event_map[key], source)

    def _handle_tap_command(self, parts: List[str]):
        try:
            x = int(parts[1])
            y = int(parts[2])
        except ValueError:
            print("[MockBackend] x/y must be int.")
            return

        source = parts[3]

        if source not in ("driver", "passenger"):
            print("[MockBackend] source must be driver or passenger.")
            return

        self._send_tap(x, y, source)

    def _handle_drag_command(self, parts: List[str]):
        try:
            x1 = int(parts[1])
            y1 = int(parts[2])
            x2 = int(parts[3])
            y2 = int(parts[4])
        except ValueError:
            print("[MockBackend] x/y must be int.")
            return

        source = parts[5]

        if source not in ("driver", "passenger"):
            print("[MockBackend] source must be driver or passenger.")
            return

        self._send_drag(x1, y1, x2, y2, source)

    def _send_tap(self, x: int, y: int, source: str):
        self.send_touch(x, y, "down", source)
        time.sleep(0.05)
        self.send_touch(x, y, "up", source)

    def _send_drag(self, start_x: int, start_y: int, end_x: int, end_y: int, source: str):
        steps = 12

        self.send_touch(start_x, start_y, "down", source)
        time.sleep(0.02)

        for i in range(1, steps):
            rate = i / steps
            x = int(start_x + (end_x - start_x) * rate)
            y = int(start_y + (end_y - start_y) * rate)

            self.send_touch(x, y, "move", source)
            time.sleep(0.02)

        self.send_touch(end_x, end_y, "up", source)

    def send_touch(self, x: int, y: int, event_type: str, source: str):
        self.send_event("EVT_TOUCH", {
            "source": source,
            "x": x,
            "y": y,
            "event": event_type
        })

    def send_event(self, event_type: str, payload: Dict):
        message = {
            self.event_field_name: event_type,
            "payload": payload,
        }

        text = json.dumps(message, separators=(",", ":"))
        self._send_line(text)

    def _send_line(self, text: str):
        line = text + "\n"
        data = line.encode("utf-8")

        removed_clients = []

        with self.clients_lock:
            if len(self.clients) == 0:
                print("[MockBackend] No Unity client connected.")
                return

            for client_socket, address in self.clients:
                try:
                    client_socket.sendall(data)
                    print(f"[MockBackend] Sent to Unity {address}: {text}")
                except OSError as error:
                    print(f"[MockBackend] Send failed to {address}: {error}")
                    removed_clients.append((client_socket, address))

            for client in removed_clients:
                self._remove_client_without_lock(client)

    def _get_message_type(self, message: Dict) -> str:
        if "message_type" in message:
            return str(message["message_type"])

        if "type" in message:
            return str(message["type"])

        return ""

    def _print_clients(self):
        with self.clients_lock:
            if len(self.clients) == 0:
                print("[MockBackend] No connected clients.")
                return

            print("[MockBackend] Connected clients:")

            for index, (_, address) in enumerate(self.clients):
                print(f"  {index}: {address}")

    def _remove_client(self, client_socket: socket.socket, address: Tuple[str, int]):
        try:
            client_socket.close()
        except OSError:
            pass

        with self.clients_lock:
            self.clients = [entry for entry in self.clients if entry[0] != client_socket]

        print(f"[MockBackend] Unity disconnected: {address}")

    def _remove_client_without_lock(self, client_entry):
        client_socket, address = client_entry

        try:
            client_socket.close()
        except OSError:
            pass

        if client_entry in self.clients:
            self.clients.remove(client_entry)

        print(f"[MockBackend] Removed client: {address}")

    def _close_all_clients(self):
        with self.clients_lock:
            for client_socket, _ in self.clients:
                try:
                    client_socket.close()
                except OSError:
                    pass

            self.clients.clear()

    def _close_server_socket(self):
        if self.server_socket is None:
            return

        try:
            self.server_socket.close()
        except OSError:
            pass

    def _print_help(self):
        print("")
        print("==== MockBackend Commands ====")
        print("Connection:")
        print("  clients")
        print("  q")
        print("")
        print("Format:")
        print("  format                 -> show current Event field name")
        print("  format message_type    -> send Events as {\"message_type\":\"...\"}")
        print("  format type            -> send Events as {\"type\":\"...\"}")
        print("")
        print("Auto status:")
        print("  auto                   -> show auto_mecha_status")
        print("  auto on                -> auto send half/full/close status after command")
        print("  auto off               -> manual status mode")
        print("")
        print("IG / Shifter:")
        print("  ig_on                  -> EVT_IG_ON")
        print("  ig_off                 -> EVT_IG_OFF")
        print("  ig_on_short            -> IG_ON")
        print("  ig_off_short           -> IG_OFF")
        print("  p                      -> gear parking")
        print("  d                      -> gear drive")
        print("  r                      -> gear reverse")
        print("")
        print("Mecha status:")
        print("  half_sts")
        print("  full_sts")
        print("  close_sts")
        print("  other_sts")
        print("")
        print("Other Events:")
        print("  hvac")
        print("  volup / voldown")
        print("  soundup / sounddown")
        print("  led_color / led_pattern")
        print("")
        print("Touch:")
        print("  td x y source          -> touch down")
        print("  tm x y source          -> touch move")
        print("  tu x y source          -> touch up")
        print("  tap x y source")
        print("  drag x1 y1 x2 y2 source")
        print("  touchd / touchp")
        print("  dragd / dragp")
        print("")
        print("Raw:")
        print("  send {json}")
        print("==============================")
        print("")


if __name__ == "__main__":
    backend = MockBackend(BIND_HOST, PORT)
    backend.start()
