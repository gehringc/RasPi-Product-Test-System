import tkinter as tk
import socket
import queue
import threading
import time
import wiringpi

class Application(tk.Frame):
    def __init__(self, master=None):
        super().__init__(master)
        self.pack()
        self.create_widgets()
        self.flagger = False
        self.datas = (0x000040).to_bytes(3,'big')

    def create_widgets(self):
        self.hi_there = tk.Button(self)
        self.hi_there["text"] = "Hello World\n(click me)"
        self.hi_there["command"] = self.say_hi
        self.hi_there.pack(side="top")

        self.cam_try = tk.Message(self)
        self.intCtrl = tk.IntVar()
        self.cam_try.pack(side="right")
        self.cam_try["textvariable"] = self.intCtrl
        #self.quit = tk.Button(self, text="QUIT", fg="red",
        #                      command=root.destroy)
        #self.quit = tk.Button(self, text="Always Update",command=self.updateMessage)
        #self.quit.pack(side="bottom")

    def say_hi(self):
        self.flagger = not self.flagger
        #self.updateMessage()
        shiftByte = self.flagger << 6
        self.datas = (0x000000 | shiftByte).to_bytes(3,'big')


    def start(self):
        self.updateMessage()
    
    def updateMessage(self):
            if not q.empty():
                recData = int.from_bytes(q.get(),'big')
                self.intCtrl.set(recData)
                q.task_done()
            q2.put(self.datas)
            q2.join()
            self.after(80, self.updateMessage)
        

ME_IP = "192.168.0.3"
ME_PORT = 9999
recSock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
recSock.bind((ME_IP, ME_PORT))
UDP_IP = "192.168.0.5"
UDP_PORT = 45555
sendSock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

def receiver():
    data, addr = recSock.recvfrom(256)
    q.put(data)
    q.join()

def sender(data):
    sendSock.sendto(data,(UDP_IP, UDP_PORT))

def worker():
    while True:
        receiver()

def keepSending():
    while True:
        if not q2.empty():
            sendData = q2.get()
            sender(sendData)
            q2.task_done()

def spiWorker():
    readCommand = (0x03).to_bytes(1,'big')
    while True:
        recvData = wiringpi.wiringPiSPIDataRW(0,readCommand)
        print(recvData)
        
q = queue.Queue()
q2 = queue.LifoQueue()
wiringpi.wiringPiSetupGpio()
wiringpi.wiringPiSPISetup(500000,0)
root = tk.Tk()
app = Application(master=root)
receiveThread = threading.Thread(target=worker)
#receiveThread.daemon = True
sendThread = threading.Thread(target=keepSending)
spiThread = threading.Thread(target=spiWorker)
#sendThread.daemon = True
sendThread.start()
receiveThread.start()
app.updateMessage()
app.mainloop()


