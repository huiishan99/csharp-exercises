import socket
import threading
from typing import List, Tuple

BIND_HOST = "0.0.0.0"
PORT = 5000


class GuiCommandMockServer:
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

        print("[GuiCommandMockServer] Started.")
        print(f"[GuiCommandMockServer] Listening target: {self.host}:{self.port}")
        print("[GuiCommandMockServer] Unityから送られたCommand JSONを表示します。")
        print("[GuiCommandMockServer] Commands:")
        print("  clients : show connected clients")
        print("  q       : quit")

        self._run_console_loop()

    def _run_server(self):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
            self.server_socket = server_socket
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind((self.host, self.port))
            server_socket.listen(8)

            print(f"[GuiCommandMockServer] Listening on {self.host}:{self.port}")

            while self.running:
                try:
                    client_socket, address = server_socket.accept()
                    client_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

                    with self.clients_lock:
                        self.clients.append((client_socket, address))

                    print(f"[GuiCommandMockServer] Unity connected: {address}")

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

                buffer += data.decode("utf-8", errors="replace")

                while "\n" in buffer:
                    line, buffer = buffer.split("\n", 1)
                    line = line.strip()

                    if line:
                        print(f"[GuiCommandMockServer] Received from {address}: {line}")
        except OSError as error:
            print(f"[GuiCommandMockServer] Client read error {address}: {error}")
        finally:
            self._remove_client(client_socket, address)

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

            print("[GuiCommandMockServer] Unknown command. Use clients / q.")

    def _print_clients(self):
        with self.clients_lock:
            if len(self.clients) == 0:
                print("[GuiCommandMockServer] No connected clients.")
                return

            print("[GuiCommandMockServer] Connected clients:")

            for index, (_, address) in enumerate(self.clients):
                print(f"  {index}: {address}")

    def _remove_client(self, client_socket: socket.socket, address: Tuple[str, int]):
        try:
            client_socket.close()
        except OSError:
            pass

        with self.clients_lock:
            self.clients = [entry for entry in self.clients if entry[0] != client_socket]

        print(f"[GuiCommandMockServer] Unity disconnected: {address}")

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
    server = GuiCommandMockServer(BIND_HOST, PORT)
    server.start()
