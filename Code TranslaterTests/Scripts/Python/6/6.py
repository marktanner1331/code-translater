import socket
import threading
import time

IP = "127.0.0.1"
PORT = 25565

server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_sock.bind((IP, PORT))
server_sock.listen()
conn, addr = server_sock.accept()

def recv():
    while True:
        income_message = conn.recv(1024)
        print("client: " + income_message.decode())

def send():
    while True:
        message =input("")
        conn.send(message.encode())
        if message == "break":
            break

def main():
    x = threading.Thread(target=recv, args=())
    x.start()
    y = threading.Thread(target=send, args=())
    y.start()

    print("started threads!")
    if not send():
        x.join()
        y.join()
        server_sock.close()
        print("stop")

if __name__ == "__main__":
    main()