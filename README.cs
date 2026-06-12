import socket
import threading
import json
import time
from typing import List, Tuple

BIND_HOST = "0.0.0.0"
PORT = 5001


class GuiEventMockServer:
    def __init__(self, host: str, port: int):
        self.host = host
        self.port = port
        self.running = True
        self.server_socket = None
        self.clients: List[Tuple[socket.socket, Tuple[str, int]]] = []
        self.clients_lock = threading.Lock()

    def start(self):
        server_thread = threading.Thread(target=self._run_server, daemon=True)
        server_thread.start()

        print("[GuiEventMockServer] Started.")
        print(f"[GuiEventMockServer] Listening target: {self.host}:{self.port}")
        print("[GuiEventMockServer] Commands:")
        print("  ig_on")
        print("  ig_off")
        print("  p / d / r")
        print("  hvac")
        print("  volup / voldown")
        print("  half_sts / full_sts / close_sts / other_sts")
        print("")
        print("Touch:")
        print("  td x y source       -> touch down")
        print("  tm x y source       -> touch move")
        print("  tu x y source       -> touch up")
        print("  tap x y source      -> touch tap")
        print("  drag x1 y1 x2 y2 source")
        print("")
        print("Samples:")
        print("  touchd / touchp")
        print("  dragd / dragp")
        print("  clients")
        print("  q")

        self._run_console_loop()

    def _run_server(self):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
            self.server_socket = server_socket
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind((self.host, self.port))
            server_socket.listen(8)

            print(f"[GuiEventMockServer] Listening on {self.host}:{self.port}")

            while self.running:
                try:
                    client_socket, address = server_socket.accept()
                    client_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

                    with self.clients_lock:
                        self.clients.append((client_socket, address))

                    print(f"[GuiEventMockServer] Unity connected: {address}")
                except OSError:
                    break

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

            if command == "clients":
                self._print_clients()
                continue

            self._handle_command(command)

    def _handle_command(self, command: str):
        if command == "ig_on":
            self.send_event("IG_ON", {})
            return

        if command == "ig_off":
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
            self._send_tap(300, 300, "passenger")
            return

        if command == "dragd":
            self._send_drag(200, 300, 700, 300, "driver")
            return

        if command == "dragp":
            self._send_drag(200, 300, 700, 300, "passenger")
            return

        parts = command.split()

        if len(parts) == 4 and parts[0] in ("td", "tm", "tu"):
            self._handle_single_touch_command(parts)
            return

        if len(parts) == 4 and parts[0] == "tap":
            self._handle_tap_command(parts)
            return

        if len(parts) == 6 and parts[0] == "drag":
            self._handle_drag_command(parts)
            return

        print("[GuiEventMockServer] Unknown command.")

    def _handle_single_touch_command(self, parts):
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
            print("[GuiEventMockServer] source must be driver or passenger.")
            return

        try:
            x = int(x_text)
            y = int(y_text)
        except ValueError:
            print("[GuiEventMockServer] x/y must be int.")
            return

        self.send_touch(x, y, event_map[key], source)

    def _handle_tap_command(self, parts):
        try:
            x = int(parts[1])
            y = int(parts[2])
        except ValueError:
            print("[GuiEventMockServer] x/y must be int.")
            return

        source = parts[3]

        if source not in ("driver", "passenger"):
            print("[GuiEventMockServer] source must be driver or passenger.")
            return

        self._send_tap(x, y, source)

    def _handle_drag_command(self, parts):
        try:
            x1 = int(parts[1])
            y1 = int(parts[2])
            x2 = int(parts[3])
            y2 = int(parts[4])
        except ValueError:
            print("[GuiEventMockServer] x/y must be int.")
            return

        source = parts[5]

        if source not in ("driver", "passenger"):
            print("[GuiEventMockServer] source must be driver or passenger.")
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

    def send_event(self, message_type: str, payload: dict):
        message = {
            "message_type": message_type,
            "payload": payload,
        }

        text = json.dumps(message, separators=(",", ":")) + "\n"
        data = text.encode("utf-8")

        removed_clients = []

        with self.clients_lock:
            if len(self.clients) == 0:
                print("[GuiEventMockServer] No Unity client connected.")
                return

            for client_socket, address in self.clients:
                try:
                    client_socket.sendall(data)
                    print(f"[GuiEventMockServer] Sent to {address}: {text.strip()}")
                except OSError as error:
                    print(f"[GuiEventMockServer] Send failed to {address}: {error}")
                    removed_clients.append((client_socket, address))

            for client in removed_clients:
                self._remove_client_without_lock(client)

    def _print_clients(self):
        with self.clients_lock:
            if len(self.clients) == 0:
                print("[GuiEventMockServer] No connected clients.")
                return

            print("[GuiEventMockServer] Connected clients:")

            for index, (_, address) in enumerate(self.clients):
                print(f"  {index}: {address}")

    def _remove_client_without_lock(self, client_entry):
        client_socket, address = client_entry

        try:
            client_socket.close()
        except OSError:
            pass

        if client_entry in self.clients:
            self.clients.remove(client_entry)

        print(f"[GuiEventMockServer] Removed client: {address}")

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


if __name__ == "__main__":
    server = GuiEventMockServer(BIND_HOST, PORT)
    server.start()
