from pythonosc import udp_client
client = udp_client.SimpleUDPClient("172.20.10.3", 9000)  # use your Unity IP:port
client.send_message("/Receiver", [50.0, 20.0, 70.0])
