import socket
import threading
import json
import time
from typing import List, Tuple


BIND_HOST = "0.0.0.0"
PORT = 5000


class TouchMockServer:
    def __init__(self, host: str, port: int):
        self.host = host
        self.port = port

        self.clients: List[Tuple[socket.socket, Tuple[str, int]]] = []
        self.clients_lock = threading.Lock()

        self.running = True
        self.server_socket = None

    def start(self):
        server_thread = threading.Thread(target=self._run_server, daemon=True)
        server_thread.start()

        print("[MockTouchServer] Started.")
        print(f"[MockTouchServer] Bind: {self.host}:{self.port}")
        print("[MockTouchServer] Commands:")
        print("  d x y source  -> down")
        print("  m x y source  -> move")
        print("  u x y source  -> up")
        print("  tapd          -> driver tap sample")
        print("  tapp          -> passenger tap sample")
        print("  dragd         -> driver drag sample")
        print("  dragp         -> passenger drag sample")
        print("  clients       -> show connected clients")
        print("  q             -> quit")
        print("")
        print("[MockTouchServer] source must be driver or passenger.")
        print("[MockTouchServer] Example: d 100 200 driver")

        self._run_console_loop()

    def _run_server(self):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
            self.server_socket = server_socket
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind((self.host, self.port))
            server_socket.listen(8)

            print(f"[MockTouchServer] Listening on {self.host}:{self.port}")

            while self.running:
                try:
                    client_socket, address = server_socket.accept()
                    client_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

                    with self.clients_lock:
                        self.clients.append((client_socket, address))

                    print(f"[MockTouchServer] Unity connected: {address}")
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
        if command == "tapd":
            self._send_tap(300, 300, "driver")
            return

        if command == "tapp":
            self._send_tap(300, 300, "passenger")
            return

        if command == "dragd":
            self._send_drag(200, 300, 700, 300, "driver")
            return

        if command == "dragp":
            self._send_drag(200, 300, 700, 300, "passenger")
            return

        parts = command.split()

        if len(parts) != 4:
            print("[MockTouchServer] Invalid command.")
            print("[MockTouchServer] Example: d 100 200 driver")
            return

        event_key = parts[0]
        x_text = parts[1]
        y_text = parts[2]
        source = parts[3]

        event_type_map = {
            "d": "down",
            "m": "move",
            "u": "up",
        }

        if event_key not in event_type_map:
            print("[MockTouchServer] event must be d / m / u.")
            return

        if source not in ("driver", "passenger"):
            print("[MockTouchServer] source must be driver / passenger.")
            return

        try:
            x = int(x_text)
            y = int(y_text)
        except ValueError:
            print("[MockTouchServer] x and y must be int.")
            return

        self.send_touch(x, y, event_type_map[event_key], source)

    def _send_tap(self, x: int, y: int, source: str):
        self.send_touch(x, y, "down", source)
        time.sleep(0.05)
        self.send_touch(x, y, "up", source)

    def _send_drag(self, start_x: int, start_y: int, end_x: int, end_y: int, source: str):
        steps = 10

        self.send_touch(start_x, start_y, "down", source)
        time.sleep(0.02)

        for i in range(1, steps):
            rate = i / steps
            x = int(start_x + (end_x - start_x) * rate)
            y = int(start_y + (end_y - start_y) * rate)

            self.send_touch(x, y, "move", source)
            time.sleep(0.03)

        self.send_touch(end_x, end_y, "up", source)

    def send_touch(self, x: int, y: int, event_type: str, source: str):
        message = {
            "x": x,
            "y": y,
            "event_type": event_type,
            "source": source,
        }

        # TCP 是 stream，所以每条 JSON 后面加换行，Unity 按行读取。
        text = json.dumps(message, separators=(",", ":")) + "\n"
        data = text.encode("utf-8")

        removed_clients = []

        with self.clients_lock:
            if len(self.clients) == 0:
                print("[MockTouchServer] No Unity client connected.")
                return

            for client_socket, address in self.clients:
                try:
                    client_socket.sendall(data)
                    print(f"[MockTouchServer] Sent to {address}: {text.strip()}")
                except OSError as error:
                    print(f"[MockTouchServer] Send failed to {address}: {error}")
                    removed_clients.append((client_socket, address))

            for client in removed_clients:
                self._remove_client_without_lock(client)

    def _print_clients(self):
        with self.clients_lock:
            if len(self.clients) == 0:
                print("[MockTouchServer] No connected clients.")
                return

            print("[MockTouchServer] Connected clients:")

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

        print(f"[MockTouchServer] Removed client: {address}")

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
    server = TouchMockServer(BIND_HOST, PORT)
    server.start()
