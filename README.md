# Janus
An experimental merger between the powers of C#, ffmpeg, and jsmpeg. Named after the Roman god for change and duality.

Current performance is exceptional, with only 3-5 frames of encode-decode latency using 1280x720@60:

![image](https://user-images.githubusercontent.com/13019172/75597068-f4dd7f00-5a58-11ea-9e4a-b59d16e748de.png)

Interop mechanism is simply using IPC pipes w/ ffmpeg and an AspNetCore websockets server to send the data up to jsmpeg.

Objectives are to investigate various UX implementations which could leverage the power of a large commodity server on a low-power device. Types of UX to investigate:

- Business administration
- 3d graphics engine (a raytracer could be compelling considering scaling potential)

Next steps could be to investigate ways of distributing the frame drawing task across multiple LAN machines. Assuming 60 fps, this gives us ~15ms budget per frame. On a LAN, there should be nearly zero overhead latency-wise. UDP would be a good potential starting point for inter-node communication transport.
