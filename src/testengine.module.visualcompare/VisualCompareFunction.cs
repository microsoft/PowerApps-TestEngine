using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using OpenCvSharp;


namespace testengine.module.visualcompare
{
    public class VisualCompareFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly IFileSystem _filesystem;
        private static readonly TableType _tableType = TableType.Empty().Add("name", FormulaType.String);

        public VisualCompareFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, IFileSystem filesystem, ILogger logger) : base(
            DPath.Root.Append(new DName("Preview")), 
            "VisualCompare", // Name
            FormulaType.Number, // Return
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
        }

        public NumberValue Execute(StringValue locator, StringValue referenceImage, TableValue compareTypes)
        {
            return ExecuteAsync(locator, referenceImage, compareTypes).Result;
        }

        public async Task<NumberValue> ExecuteAsync(StringValue locator, StringValue referenceImage, TableValue compareTypes)
        {
            if (string.IsNullOrEmpty(locator.Value))
            {
                _logger.LogError("locator cannot be empty.");
                throw new ArgumentException();
            }

            // Convert relative path to path relative to test file
            var filename = GetFullFile(_testState, referenceImage.Value);

            if (!_filesystem.FileExists(filename))
            {
                _logger.LogError("Invalid file");
                throw new ArgumentException("Invalid file");
            }

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

            var file = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + $"_{DateTime.Now.ToString("yyyyMMddHHmm")}" + Path.GetExtension(filename));

            // Save the locator region to a file
            SaveLocatorRegionToFile(locatorRegion, file);

            string base64String = _filesystem.ReadAllText(filename);
            byte[] referenceImageData = Convert.FromBase64String(base64String);

            foreach (var row in compareTypes.Rows)
            {
                foreach (var column in row.Value.Fields)
                {
                    Console.WriteLine($"Compare Type: {column.Value.ToObject()}");
                }
            }

            // Call the ProcessImageCompare method
            var similarity = ProcessImageCompare(locator.Value, referenceImageData, compareTypes.Rows.Select(r => r.Value.Fields.First().Value.ToObject().ToString()).ToArray());

            // Return a perentage similarity value 0 - 100% where 100% is a perfect match
            return FormulaValue.New(similarity);
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

        private float ProcessImageCompare(string locator, byte[] referenceImage, string[] compareTypes)
        {
            Console.WriteLine($"Reference Image: {referenceImage}");
            Console.WriteLine($"Tolerance: {tolerance}");

            foreach (var compareType in compareTypes)
            {
                Console.WriteLine($"Compare Type: {compareType}");
            }

            // Load the input image from locator snapshop image
            // TODO: Save region to file

            Mat inputImage = Cv2.ImRead("Region.png", ImreadModes.Color);
            Mat refImage = Cv2.ImDecode(referenceImage, ImreadModes.Color);

            // Check if the images were successfully loaded
            if (inputImage.Empty() || refImage.Empty())
            {
                Console.WriteLine($"Error: Unable to load images.");
                return;
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
                    default:
                        Console.WriteLine($"Invalid compare type: {compareType}");
                        break;
                }
            }

            // Save the result to the output path
            Cv2.ImWrite("output_image_compare.png", extractedRegion);

            Console.WriteLine($"Image comparison completed. Output saved at output_image_compare.png");
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

            // Return the rectangle of the element
            return new Rect((int)boundingBox.X, (int)boundingBox.Y, (int)boundingBox.Width, (int)boundingBox.Height);
        }
    }
}
