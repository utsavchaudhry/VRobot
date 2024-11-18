===============================
Conference Video Chat Demo
===============================
Features:
- Multiple participant support
- Local testing with 3 built-in instances
- Peer-to-peer connections for efficient data transfer
- Scalable to connect with multiple external devices

===============================
Test Scenario:
===============================
1. Open the scene in Unity Editor and run
2. Three instances of the example app are visible on the screen
3. Enter the same passphrase in all three instances
4. Tick "Video" only for the first instance (most platforms allow only one app to access a video 
   device)
5. Press "Join" on all three apps
Result: One app will send video to all others.
You can also run this app on other devices and platforms to test sending & receiving between them.
For audio testing, use headphones or separate rooms to avoid audio feedback.

===============================
Pitfalls:
===============================
* The number of concurrent connections is limited by CPU, memory and network bandwidth of the 
  weakest device in your conference chat. Video resolution in particular has a big impact. To 
  avoid problems decide on minimal hardware requirements and set a max. limit on user number & 
  devices to ensure it can run stable. 
* When a user joins a conference it will automatically create peer connections to all other users. 
  This connection process can become unstable with large numbers of users because the connections 
  can compete with each other over resources. 
* The conference only operates on the level of peer connections. If a peer connection fails 
  between two users it isn't clear if a user simply left or a network error led to the connection 
  being cut. Two users with a failed peer connection between them simply won't be able to see / 
  hear each other. It is recommended to keep the number of concurrent users low to avoid such 
  issues. A separate connection (e.g. photon) is recommended to let users know who is expected to
  connect to who and better detect any connection problems.