![NexaCast Video](https://raw.githubusercontent.com/BConley995/NexaCastVideo/master/Assets/Banner.png)

# NexaCast-Video: Automated YouTube Video Generator

NexaCast is an innovative project that leverages advanced AI technologies to automate the creation of YouTube videos. It enables users to input a brief summary or idea, and NexaCast efficiently generates a comprehensive video. This includes creating scripts, voiceovers, visuals, and adding background music, all tailored for YouTube audiences.

- [NexaCast on YouTube](https://www.youtube.com/channel/UC2NPaEM6nPwXMSgdWJ8WOwg) - Explore our creations and progress on our YouTube channel.
- [How NexaCast Works](https://www.youtube.com/watch?v=Wq6uoHomrig&t=1s) - Watch this video to understand how NexaCast automates video generation for YouTube.

## Project Overview

NexaCast-Video combines various technologies and APIs to create a seamless video production workflow. The application interacts with services like ChatGPT, DALL·E, and Eleven Labs to generate content that is then compiled into a complete video.

### Key Features

- **Asynchronous Operations**: Enhances efficiency by handling multiple tasks concurrently.
- **Dynamic Content Creation**: Uses AI to generate scripts, images, and voiceovers based on user input.
- **Background Music Integration**: Adds royalty-free music to videos, with features like looping and fading effects.
- **Logging System**: Records important events, errors, or invalid inputs for debugging and monitoring.
- **User Input Validation**: Employs Regex for content adherence to YouTube's guidelines.

## Current Capabilities

- **Topic Generation**: Utilizes ChatGPT to derive relevant topics from user inputs.
- **Script Creation**: Crafts detailed video scripts based on the chosen topic, which can take up to 10 minutes.
- **Voiceover Production**: Converts script lines into engaging voiceovers.
- **Dynamic Image Sourcing**: Generates contextually relevant images for each script line using DALL·E.
- **Royalty-Free Music**: Sources random royalty-free music tracks for the video background.

## Development Progress

The project is in an active development phase, with the following functionalities implemented:

- **Video Compilation**: Combines generated content into a cohesive video format.
- **Music Integration**: Adds and adjusts background music, including looping and fading effects.

## Installation and Setup

1. **Clone the Repository**: Download the project from its GitHub repository.
2. **API Keys**: Store your API keys (ChatGPT, Eleven Labs, DALL·E, Google Cloud) in `.\SECURE\config.json`.
3. **NuGet Packages**: Install required NuGet packages.
   Required Nuget Packages
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
    - Xabe.FFmpeg               5.2.6

4. **FFmpeg**: Ensure FFmpeg is installed and configured, as it is crucial for video and audio processing.
5. **Running**:  Once you run the application in Visual Studio, it will generate the content in `.\NexaCastVideo\bin\Debug\net6.0-windows\NexaCastVideo\Generation\{Name From Prompt}`

## Usage Guide

NexaCast-Video is designed to be user-friendly:

1. **Input Topic**: Users can input a brief summary or topic idea.
2. **Content Generation**: The application generates a script, sources images, and produces voiceovers.
3. **Video Compilation**: Combines all elements into a final video, adding background music and ensuring optimal length.
4. **Review and Upload**: Users can review the final video before uploading it to YouTube.

## Future Enhancements

- **Front-End Development**: Create a user-friendly interface for easier interaction.
- **Automated YouTube Uploads**: Directly upload completed videos to YouTube.
- **Continuous Optimization**: Regular updates for performance improvements.
- **Comprehensive Documentation**: Detailed guides and API documentation for developers and users.
