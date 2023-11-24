# NexaCast-Video: Automated YouTube Video Generator

NexaCast is a cutting-edge project that utilizes advanced AI technologies to automate the creation of videos for YouTube. Users can input a brief summary or idea, and NexaCast efficiently generates a comprehensive video, including visuals, voiceovers, subtitles, and background music, tailored for YouTube audiences.

## Project Overview

### Features to Integrate:

1. Logging system to record errors, invalid inputs, or other important events.
1. Asynchronous operations to enhance efficiency.
1. Unit tests to ensure robustness.
1. SOLID principle comments for maintainability and clarity.
1. Regex validation for ensuring content adheres to YouTube's guidelines.

## Current Capabilities

- **Topic Generation**: NexaCast employs ChatGPT to generate relevant topics based on user inputs. 
- **Script Creation**: It crafts a detailed video script using the derived topic. (Can take 10 minutes to do this portion)
- **Voiceover Production**: Converts each script line into engaging voiceovers.
- **Dynamic Image Sourcing**: Uses DALL·E to generate contextually relevant images for each script line.
- **Royalty-Free Music**: Pulls random royalty-free music for the video's background.

Currently, NexaCast-Video can generate the script, images, text-to-speech voiceover, and select royalty-free music. The development is ongoing to enable the following:

- Compiling the video into a cohesive format.
- Shortening the video length to fit YouTube standards.

Explore the AI creations and progress on the YouTube channel: [NexaCastVideo on YouTube](https://www.youtube.com/@NexaCastVideo)

## Installation

1. Clone this repository.
1. Obtain and store your API keys in `.\SECURE\config.json`, including keys for ChatGPT, Eleven Labs, DALL·E, and Google Cloud.
1. Required Nuget Packages
    - Accord by Accord.NET      3.8.0
    - Google.Apis.Drive.v3      1.64.0.3155
    - Microsoft.Win32.Registry  5.0.0
    - Google.Apis.YouTube.v3    1.64.0.3205
    - NAudio                    2.2.1
    - NAudio.Asio               2.2.1
    - NAudio.Core               2.2.1
    - NAudio.Midi               2.2.1
    - NAudio.Wasapi             2.2.1
    - NAudio.WinForms           2.2.1
    - Newtonsoft.Json           13.0.3
    - Polly                     8.2.0
    - SixLabors.ImageSharp      2.1.4
    - System.Drawing.Common     8.0.0
    - System.Text.Json          8.0.0
    - Xabe.FFmpeg                5.2.6
