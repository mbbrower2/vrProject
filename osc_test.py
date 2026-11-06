from pythonosc import udp_client
client = udp_client.SimpleUDPClient("10.5.9.139", 7000)  # use your Unity IP:port
client.send_message("/test", [True])