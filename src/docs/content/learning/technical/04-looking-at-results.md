---
title: 04 - Looking at Results
---

After running your tests, it's important to understand how to interpret the results. The Power Apps Test Engine generates a detailed output that helps you analyze the performance and correctness of your tests.

## Test Output

When you run your tests, the Power Apps Test Engine creates a `TestOutput` folder. This inside this folder is named with a unique date and time stamp to help you easily identify when the tests were executed. Inside this folder, you'll find several important files and subfolders:

### Completed Test Results File

The main file you'll find in the `TestOutput` folder is the completed test results file with a `.trx` extension. This file contains a summary of all the tests that were run, including information on which tests passed, which failed, and any errors that occurred.

### Test Case Folders

For each test case, the Power Apps Test Engine creates a separate folder within the `TestOutput` directory. These folders are named after the individual test cases and contain the following:

- **Video Recording**: A video recording of the test execution. This is particularly useful for visualizing what happened during the test and for debugging purposes.
- **Log Files**: Detailed log files that provide step-by-step information about the test execution. These logs can help you understand the sequence of actions performed during the test and identify any issues that may have occurred.

## Analyzing the Results

1. **Open the `TestOutput` Folder**: Navigate to the `TestOutput` folder created during the test run. The folder name will include the date and time of the test execution.
2. **Review the `.trx` File**: Open the `.trx` file to get an overview of the test results. This file will show you which tests passed, which failed, and provide details on any errors.
3. **Examine Individual Test Case Folders**: For more detailed analysis, open the folders for individual test cases. Watch the video recordings to see exactly what happened during the test, and review the log files for a detailed breakdown of the test steps.

By thoroughly examining the contents of the `TestOutput` folder, you can gain valuable insights into the performance and reliability of your Power Platform solutions. This detailed analysis helps you identify and fix issues, ensuring your applications are robust and reliable.
