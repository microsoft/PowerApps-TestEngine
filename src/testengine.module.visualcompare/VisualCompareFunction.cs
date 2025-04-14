// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace testengine.module.visualcompare
{
    public class VisualCompareFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState? _testState;
        private readonly ILogger _logger;
        private readonly IFileSystem _filesystem;
        private readonly ISingleTestInstanceState? _singleTestState;
        private static readonly RecordType _metrics = RecordType.Empty()
            .Add(new NamedFormulaType("Similarity", NumberType.Number))
            .Add(new NamedFormulaType("Luminance", NumberType.Number))
            .Add(new NamedFormulaType("Contrast", NumberType.Number))
            .Add(new NamedFormulaType("Structure", NumberType.Number));

        private static readonly TableType _tableType = TableType.Empty()
            .Add("type", FormulaType.String);

        public VisualCompareFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, IFileSystem filesystem, ILogger logger) : base(
            DPath.Root.Append(new DName("Preview")),
            "VisualCompare", // Name
            _metrics, // Return
            FormulaType.String, // Locator
            FormulaType.String, // referenceImage
           _tableType // CompareType
            )
        {
            // Initialize the class variables
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _filesystem = filesystem;
            _testState = testState;
            if (_testState != null && _testState.TestProvider != null && _testState.TestProvider.SingleTestInstanceState != null)
            {
                _singleTestState = _testState.TestProvider.SingleTestInstanceState;
            }
            else
            {
                _singleTestState = null;
            }
        }

        public RecordValue Execute(StringValue locator, StringValue referenceImage, TableValue compareTypes)
        {
            return ExecuteAsync(locator, referenceImage, compareTypes).Result;
        }

        public async Task<RecordValue> ExecuteAsync(StringValue locator, StringValue referenceImage, TableValue compareTypes)
        {
            if (string.IsNullOrEmpty(locator.Value))
            {
                _logger.LogError("locator cannot be empty.");
                throw new ArgumentException();
            }

            // Convert relative path to path relative to test file
            var filename = GetFullFile(_testState, referenceImage.Value);

            IPage page = _testInfraFunctions.GetContext().Pages.First();

            if (page.Url.ToString() == "about:blank" && _testInfraFunctions.GetContext().Pages.Count() >= 2)
            {
                _logger.LogInformation("Skipping blank first page");
                page = _testInfraFunctions.GetContext().Pages.Skip(1).First();
            }

            // Get the locator region
            var locatorRegion = await GetLocatorRegionAsync(page, locator.Value);
            if (locatorRegion == null)
            {
                _logger.LogError("Unable to find locator region.");
                throw new ArgumentException("Invalid locator");
            }

            var file = GetOutputFile(_testState, Path.GetFileNameWithoutExtension(filename) + $"_{DateTime.Now.ToString("yyyyMMddHHmm")}" + Path.GetExtension(filename));

            // Save the locator region to a file
            var inputImageData = await SaveLocatorRegionToFileAsync(page, locatorRegion, file);

            byte[] referenceImageData = new byte[] { };

            if (_filesystem.FileExists(filename))
            {
                string base64String = _filesystem.ReadAllText(filename);
                referenceImageData = Convert.FromBase64String(base64String);
            }

            foreach (var row in compareTypes.Rows)
            {
                foreach (var column in row.Value.Fields)
                {
                    Console.WriteLine($"Compare Type: {column.Value.ToObject()}");
                }
            }

            // Call the ProcessImageCompare method
            return ProcessImageCompare(locator.Value, filename, inputImageData, referenceImageData, compareTypes.Rows.Select(r => r.Value.Fields.First().Value.ToObject().ToString()).ToArray());
        }

        public async Task<byte[]> SaveLocatorRegionToFileAsync(IPage page, Rect region, string filename)
        {
            // Take a screenshot of the entire page
            var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true
            });

            // Load the screenshot into an OpenCV Mat object
            Mat fullImage = Cv2.ImDecode(bytes, ImreadModes.Color);

            // Extract the specified region from the screenshot
            Rect extractedRegionRect = new Rect(region.X, region.Y, region.Width, region.Height);
            Mat extractedRegion = new Mat(fullImage, extractedRegionRect);

            // Convert the extracted region to a byte array
            byte[] extractedBytes = extractedRegion.ToBytes(".png");

            // Save the extracted region to the specified file
            _filesystem.WriteFile(filename, extractedBytes);

            // Return the extracted image region as a byte array
            return extractedBytes;
        }

        private string GetFullFile(ITestState testState, string filename)
        {
            if (!Path.IsPathRooted(filename))
            {
                var testResultDirectory = Path.GetDirectoryName(testState.GetTestConfigFile().FullName);
                filename = Path.Combine(testResultDirectory, filename);
            }
            return filename;
        }

        private string GetOutputFile(ITestState testState, string filename)
        {
            if (!Path.IsPathRooted(filename))
            {
                filename = Path.Combine(_singleTestState.GetTestResultsDirectory(), filename);
            }
            return filename;
        }

        private RecordValue ProcessImageCompare(string locator, string baseFile, byte[] inputDataImage, byte[] referenceImage, string[] compareTypes)
        {
            Console.WriteLine($"Reference Image: {referenceImage}");

            foreach (var compareType in compareTypes)
            {
                Console.WriteLine($"Compare Type: {compareType}");
            }

            Mat inputImage = Cv2.ImDecode(inputDataImage, ImreadModes.Color);
            Mat refImage = referenceImage != null && referenceImage.Length > 0 ? Cv2.ImDecode(referenceImage, ImreadModes.Color) : new Mat();

            // Check if the images were successfully loaded
            if (inputImage.Empty())
            {
                throw new InvalidDataException($"Error: Unable to load input image.");
            }

            // Extract the region from the locator region match (for demonstration, using the entire image)
            Rect region = new Rect(0, 0, inputImage.Width, inputImage.Height);
            Mat extractedRegion = new Mat(inputImage, region);

            // Apply the specified image filters
            foreach (var compareType in compareTypes)
            {
                switch (compareType.ToLower())
                {
                    case "edge":
                        Mat edges = new Mat();
                        Cv2.Canny(extractedRegion, edges, 50, 150);
                        extractedRegion = edges;
                        break;
                    case "color":
                        Mat colorMap = new Mat();
                        Cv2.CvtColor(extractedRegion, colorMap, ColorConversionCodes.BGR2GRAY);
                        Cv2.ApplyColorMap(colorMap, colorMap, ColormapTypes.Jet);
                        extractedRegion = colorMap;
                        break;
                    case "simplify":
                        Mat blurred = new Mat();
                        Cv2.GaussianBlur(extractedRegion, blurred, new Size(21, 21), 0);
                        extractedRegion = blurred;
                        break;
                    case "feature":
                        Mat gray = new Mat();
                        Cv2.CvtColor(extractedRegion, gray, ColorConversionCodes.BGR2GRAY);
                        var sift = SIFT.Create();
                        KeyPoint[] keypoints;
                        Mat descriptors = new Mat();
                        sift.DetectAndCompute(gray, null, out keypoints, descriptors);
                        Mat outputImage = new Mat();
                        Cv2.DrawKeypoints(extractedRegion, keypoints, outputImage, Scalar.LimeGreen, DrawMatchesFlags.DrawRichKeypoints);
                        extractedRegion = outputImage;
                        break;
                    case "fourier":
                        Mat imageFloat = new Mat();
                        extractedRegion.ConvertTo(imageFloat, MatType.CV_32F);
                        Mat complexImage = new Mat();
                        Cv2.Dft(imageFloat, complexImage, DftFlags.ComplexOutput);
                        Mat shiftedImage = new Mat();
                        Cv2.Dft(complexImage, shiftedImage, DftFlags.Inverse | DftFlags.RealOutput);
                        extractedRegion = shiftedImage;
                        break;
                    case "cosine":
                        Mat imageFloatCosine = new Mat();
                        extractedRegion.ConvertTo(imageFloatCosine, MatType.CV_32F);
                        Mat dctImage = new Mat();
                        Cv2.Dct(imageFloatCosine, dctImage);
                        extractedRegion = dctImage;
                        break;
                    case "colorsimplify":
                        Mat pixels = extractedRegion.Reshape(1, extractedRegion.Rows * extractedRegion.Cols);
                        Mat pixelsFloat = new Mat();
                        pixels.ConvertTo(pixelsFloat, MatType.CV_32F);
                        int numColors = 16;
                        Mat labels = new Mat();
                        Mat centers = new Mat();
                        Cv2.Kmeans(pixelsFloat, numColors, labels, new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 10, 1.0), 3, KMeansFlags.PpCenters, centers);
                        Mat simplifiedImage = new Mat(extractedRegion.Size(), extractedRegion.Type());
                        for (int i = 0; i < labels.Rows; i++)
                        {
                            int clusterIdx = labels.At<int>(i);
                            simplifiedImage.Set(i / extractedRegion.Cols, i % extractedRegion.Cols, centers.At<Vec3b>(clusterIdx));
                        }
                        extractedRegion = simplifiedImage;
                        break;
                }
            }

            Cv2.ImEncode(".png", extractedRegion, out byte[] byteArray);

            var compareFile = GetOutputFile(_testState, Path.GetFileNameWithoutExtension(baseFile) + "_compare.png");

            // Save the result to the output path
            _filesystem.WriteFile(compareFile, byteArray);

            Console.WriteLine($"Image comparison completed. Output saved at output_image_compare.png");

            // Check if the images were successfully loaded
            if (refImage.Empty())
            {
                throw new InvalidDataException($"Error: Unable to load reference image.");
            }

            // Calculate the comparision metrics
            return CalculateMetrics(inputImage, refImage);
        }

        private RecordValue CalculateMetrics(Mat inputImage, Mat refImage)
        {
            // Convert images to grayscale
            Mat grayInput = new Mat();
            Mat grayRef = new Mat();
            Cv2.CvtColor(inputImage, grayInput, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(refImage, grayRef, ColorConversionCodes.BGR2GRAY);

            // Calculate SSIM
            Mat ssimMap = new Mat();
            CalculateSSIM(grayInput, grayRef,
                out double ssim,
                out double luminance,
                out double contrast,
                out double structure);

            // Create a Power Fx Record with the metrics
            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("Similarity", FormulaValue.New(ssim)),
                new NamedValue("Luminance", FormulaValue.New(luminance)),
                new NamedValue("Contrast", FormulaValue.New(contrast)),
                new NamedValue("Structure", FormulaValue.New(structure))
            );

            // Return the record
            return record;
        }

        private void CalculateSSIM(Mat grayInput, Mat grayRef, out double ssim, out double luminance, out double contrast, out double structure)
        {
            // Define constants for SSIM calculation
            // C1 and C2 are constants used to stabilize the division with weak denominators.
            // They are derived from the dynamic range of the pixel values.
            const double C1 = 6.5025, C2 = 58.5225;

            // Calculate the mean of the grayscale images using GaussianBlur
            // GaussianBlur is used to smooth the image and reduce noise.
            // The kernel size is 11x11 and the standard deviation is 1.5.
            Mat mu1 = new Mat(), mu2 = new Mat();
            Cv2.GaussianBlur(grayInput, mu1, new Size(11, 11), 1.5);
            Cv2.GaussianBlur(grayRef, mu2, new Size(11, 11), 1.5);

            // Calculate the squares of the means
            // mu1_2 is the square of mu1
            // mu2_2 is the square of mu2
            // mu1_mu2 is the product of mu1 and mu2
            Mat mu1_2 = mu1.Mul(mu1);
            Mat mu2_2 = mu2.Mul(mu2);
            Mat mu1_mu2 = mu1.Mul(mu2);

            // Calculate the variance and covariance
            // sigma1_2 is the variance of grayInput
            // sigma2_2 is the variance of grayRef
            // sigma12 is the covariance of grayInput and grayRef
            // These are calculated by applying a Gaussian blur to the squared images and then subtracting the squared means.
            Mat sigma1_2 = new Mat(), sigma2_2 = new Mat(), sigma12 = new Mat();
            Cv2.GaussianBlur(grayInput.Mul(grayInput), sigma1_2, new Size(11, 11), 1.5);
            Cv2.GaussianBlur(grayRef.Mul(grayRef), sigma2_2, new Size(11, 11), 1.5);
            Cv2.GaussianBlur(grayInput.Mul(grayRef), sigma12, new Size(11, 11), 1.5);

            sigma1_2 -= mu1_2;
            sigma2_2 -= mu2_2;
            sigma12 -= mu1_mu2;

            // Calculate the SSIM map
            // t1, t2, and t3 are intermediate matrices used to calculate the SSIM map.
            Mat t1 = new Mat(), t2 = new Mat(), t3 = new Mat();
            Cv2.Add(mu1_mu2 * 2, C1, t1);
            Cv2.Add(sigma12 * 2, C2, t2);
            t3 = t1.Mul(t2);

            Cv2.Add(mu1_2 + mu2_2, C1, t1);
            Cv2.Add(sigma1_2 + sigma2_2, C2, t2);
            t1 = t1.Mul(t2);

            Mat ssimMap = new Mat();
            Cv2.Divide(t3, t1, ssimMap);

            // Calculate the mean SSIM value
            // meanSSIM is the mean value of the SSIM map, representing the overall SSIM value between the two images.
            Scalar meanSSIM = Cv2.Mean(ssimMap);

            // Calculate luminance, contrast, and structure components
            // Luminance is calculated using the means of mu1 and mu2.
            luminance = (2 * Cv2.Mean(mu1).Val0 * Cv2.Mean(mu2).Val0 + C1) / (Cv2.Mean(mu1_2).Val0 + Cv2.Mean(mu2_2).Val0 + C1);

            // Contrast is calculated using the square roots of the means of sigma1_2 and sigma2_2.
            contrast = (2 * Math.Sqrt(Cv2.Mean(sigma1_2).Val0) * Math.Sqrt(Cv2.Mean(sigma2_2).Val0) + C2) /
                       (Math.Sqrt(Cv2.Mean(sigma1_2).Val0) + Math.Sqrt(Cv2.Mean(sigma2_2).Val0) + C2);

            // Structure is calculated using the mean of sigma12.
            structure = (Cv2.Mean(sigma12).Val0 + C2) / (Math.Sqrt(Cv2.Mean(sigma1_2).Val0) * Math.Sqrt(Cv2.Mean(sigma2_2).Val0) + C2);

            // Assign the SSIM value to the out variable
            ssim = meanSSIM.Val0;
        }

        private async Task<Rect> GetLocatorRegionAsync(IPage page, string locator)
        {
            // Find the first match using the locator
            var elementHandle = await page.QuerySelectorAsync(locator);

            // Check if the element was found
            if (elementHandle == null)
            {
                throw new Exception("Element not found");
            }

            // Get the bounding box of the element
            var boundingBox = await elementHandle.BoundingBoxAsync();

            // Check if the bounding box was retrieved
            if (boundingBox == null)
            {
                throw new Exception("Unable to retrieve bounding box");
            }

            // Get the viewport size
            var viewportSize = page.ViewportSize;

            // Clip the rectangle to the visible IPage size
            var clippedX = Math.Max(0, boundingBox.X);
            var clippedY = Math.Max(0, boundingBox.Y);
            var clippedWidth = Math.Min(viewportSize.Width - clippedX, boundingBox.Width);
            var clippedHeight = Math.Min(viewportSize.Height - clippedY, boundingBox.Height);

            // Return the clipped rectangle
            return new Rect((int)clippedX, (int)clippedY, (int)clippedWidth, (int)clippedHeight);
        }
    }
}
