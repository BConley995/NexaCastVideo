# NexaCast-Video: Automated YouTube Video Generator

NexaCast is an innovative project harnessing the power of various AI technologies to automate the process of video creation for YouTube. By simply inputting a brief summary or idea, NexaCast leverages advanced AI techniques to generate a full-fledged video, complete with visuals, voiceovers, subtitles, and background music.

## Project Plan

### **Project Plan: NexaCast - The Automated YouTube Channel Creator**

---

### **1. Project Overview**

**Description:** NexaCast is an automated tool designed to create, curate, and upload content to a YouTube channel. The system leverages various APIs for script generation, video creation, voiceovers, subtitles, and more. The final product will be an intuitive interface where users can input a description of a video they would like to generate.

**Features to Integrate:**
1. Logging system to record errors, invalid inputs, or other important events.
2. Asynchronous operations to enhance efficiency.
3. Database with related tables to store video metadata and user configurations using raw SQL.
4. Unit tests to ensure robustness.
5. SOLID principle comments for maintainability and clarity.
6. Regex validation for ensuring content adheres to YouTube's guidelines.
7. Transform application into an API for potential integrations.

---

### **2. Technical Insight**

**Tools & Technologies:**
1. **Language:** C#
2. **Database:** Raw SQL for relational database management.
3. **External APIs:** 
   - Script Generation API (ChatGPT)
   - Image Generation API (DALL·E)
   - Voiceover API (Eleven Labs)
   - Royalty Free Music - Cloud Storage API (Google Drive)
   - Video Hosting API (YouTube)
4. **Others:** Regex for validation, .NET's built-in asynchronous operations, and a logging library for C#.

**Integration Plan:**
1. **Database Setup:** Using raw SQL, design two primary tables: Videos and UserConfig. Ensure relationships are defined.
2. **API Integrations:** Implement wrappers for each external API ensuring they're called asynchronously.
3. **Logging System:** After each significant step or API call, check for errors or unexpected results and write to the log.
4. **Content Validation:** 
    - **Title Validation:** Ensure generated titles are within YouTube's 100-character limit and free from forbidden characters.
    - **Description Validation:** Validate that generated descriptions are within the 5,000-character constraint.
    - **Tag Validation:** Check generated tags for the 30-character individual limit and 500-character total limit.
    - **File Name Validation:** Ensure generated image and video files have appropriate naming conventions and extensions.
5. **Unit Testing:** Prioritize tests for API integrations, database operations using raw SQL, and content validation.

---

### **3. Solicit Mentor Feedback**

I will be sharing this plan with my mentor to ensure alignment with project goals and to receive feedback on potential areas of improvement.

---

### **4. Project Plan Format**

This document aims to clearly and concisely convey the intentions behind NexaCast. While this isn't a polished mockup, it offers a foundational roadmap to guide development. Feedback and iterations are welcome to better refine the project's direction.

---

### **Submission**

I will submit the project plan via the provided Google Form before Midnight EST on Sunday, 10/29/23.


## Features

- **Topic Generation**: Based on user input, NexaCast uses ChatGPT to derive a relevant topic for the video.
  
- **Script Creation**: Using the topic, the AI crafts a comprehensive video script.
  
- **Voiceover Production**: Each script line is transformed into engaging voiceovers.
  
- **Dynamic Image Sourcing**: DALL·E is employed to generate contextual images for every script line.
  
- **Slideshow Compilation**: A visually compelling slideshow is created, aligning images with the corresponding voiceover.
  
- **Subtitle Integration**: Subtitles are auto-generated and seamlessly overlaid onto the video.
  
- **Musical Undertone**: Royalty-free background music is infused to enrich the video's ambiance.
  
- **Final Video Assembly**: All video components are combined into a final MP4 video file.

## Installation

1. Clone this repository.
2. Navigate to the project's root directory.
3. Execute the provided PowerShell script "InstallPackages" to install the necessary NuGet packages.
4. You will need to obtain your own API keys. Store them in `.\SECURE\config.json`. Specifically, you'll need API keys for:
   - ChatGPT
   - Eleven Labs
   - DALL·E
   - Google Cloud
   - YouTube


## Retrieving Large Files

NexaCast-Video uses Git Large File Storage (Git LFS) to manage large files within the repository. Before you can work with these large files, you need to ensure Git LFS is set up correctly:

### Prerequisites

1. Install Git LFS by following the instructions on the [official Git LFS website](https://git-lfs.github.com/).

### Retrieving the Large Files

After cloning the repository, follow these steps:

1. Navigate to the cloned repository's directory.

2. Ensure Git LFS is initialized for the repo:
   ```bash
   git lfs install
   ```

3. Pull the large files:
   ```bash
   git lfs pull
   ```

You should now have access to all large files, including `ffmpeg.exe`, and they should function as intended in the project.

## TODO List

1. **API Integration**:
   - Obtain and integrate API keys for DALL·E, ChatGPT, and Eleven Labs.
   - Setup cloud storage (e.g., AWS S3 or Google Cloud) and integrate the SDK for storing and retrieving music or other assets.

2. **Database Setup** (if later decided):
   - Design the database schema.
   - Configure the database connection.
   - Incorporate data handling within the application.

3. **Error Handling & Logging**:
   - Implement comprehensive error handling.
   - Set up a logging system to capture and document any issues or important events.

4. **Front-End/UI** (optional):
   - Design and implement a user-friendly interface for easier interaction.

5. **Automated Testing**:
   - Write unit and integration tests to ensure reliability and stability.

6. **Optimization**:
   - Periodically review and optimize the codebase for performance and maintainability.

7. **Documentation**:
   - Write in-depth API and developer documentation for future reference and scalability.
# NexaCastVideo
