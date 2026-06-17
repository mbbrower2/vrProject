from pythonosc import udp_client
client = udp_client.SimpleUDPClient("192.168.0.26", 7000)  # use your Unity IP:port
client.send_message("/test", [True])