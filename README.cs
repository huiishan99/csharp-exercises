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
        print("  ig_on       -> send IG_ON")
        print("  ig_off      -> send IG_OFF")
        print("  p           -> send EVT_SHIFTER_CHANGED parking")
        print("  d           -> send EVT_SHIFTER_CHANGED drive")
        print("  r           -> send EVT_SHIFTER_CHANGED reverse")
        print("  hvac        -> send EVT_HVAC_POPUP")
        print("  volup       -> send EVT_MEDIA_VOLUME_UP")
        print("  voldown     -> send EVT_MEDIA_VOLUME_DOWN")
        print("  touchd      -> send driver tap")
        print("  touchp      -> send passenger tap")
        print("  dragd       -> send driver drag")
        print("  dragp       -> send passenger drag")
        print("  half_sts    -> send half_mode_sts")
        print("  full_sts    -> send full_mode_sts")
        print("  close_sts   -> send close_mode_sts")
        print("  other_sts   -> send other_mode_sts")
        print("  clients     -> show connected clients")
        print("  q           -> quit")
        print("")
        print("Manual test flow:")
        print("  ig_on -> half_sts -> wait 3.5 sec -> full_sts")
        print("  d -> half_sts")
        print("  p -> full_sts")
        print("  r -> half_sts")
        print("  ig_off -> close_sts")

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
            # Backend側の短縮表記。Unity側では EVT_IG_ON も IG_ON も受ける。
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

        if command == "touchd":
            self._send_tap(300, 300, "driver")
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

        print("[GuiEventMockServer] Unknown command.")

    def _send_tap(self, x: int, y: int, source: str):
        self.send_event("EVT_TOUCH", {
            "source": source,
            "x": x,
            "y": y,
            "event": "down"
        })
        time.sleep(0.05)
        self.send_event("EVT_TOUCH", {
            "source": source,
            "x": x,
            "y": y,
            "event": "up"
        })

    def _send_drag(self, start_x: int, start_y: int, end_x: int, end_y: int, source: str):
        steps = 10

        self.send_event("EVT_TOUCH", {
            "source": source,
            "x": start_x,
            "y": start_y,
            "event": "down"
        })

        time.sleep(0.02)

        for i in range(1, steps):
            rate = i / steps
            x = int(start_x + (end_x - start_x) * rate)
            y = int(start_y + (end_y - start_y) * rate)

            self.send_event("EVT_TOUCH", {
                "source": source,
                "x": x,
                "y": y,
                "event": "move"
            })

            time.sleep(0.03)

        self.send_event("EVT_TOUCH", {
            "source": source,
            "x": end_x,
            "y": end_y,
            "event": "up"
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
