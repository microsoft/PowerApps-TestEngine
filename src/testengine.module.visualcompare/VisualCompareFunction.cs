using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using OpenCvSharp;


namespace testengine.module.visualcompare
{
    public class VisualCompareFunction : ReflectionFunction
    {
        public VisualCompareFunction() : base(DPath.Root.Append(new DName("Preview")), "ImageCompare", FormulaType.Blank)
        {
        }

        public BlankValue Execute(StringValue tab, StringValue referenceImage, TableValue compareTypes, NumberValue tolerance)
        {
            Console.WriteLine($"Tab: {tab.Value}");
            Console.WriteLine($"Reference Image: {referenceImage.Value}");
            Console.WriteLine($"Tolerance: {tolerance.Value}");

            foreach (var row in compareTypes.Rows)
            {
                foreach (var column in row.Value.Fields)
                {
                    Console.WriteLine($"Compare Type: {column.Value.ToObject()}");
                }
            }

            // Call the ProcessImageCompare method
            ProcessImageCompare(tab.Value, referenceImage.Value, compareTypes.Rows.Select(r => r.Value.Fields.First().Value.ToObject().ToString()).ToArray(), (int)tolerance.Value);

            return BlankValue.NewBlank();
        }

        private void ProcessImageCompare(string tab, string referenceImage, string[] compareTypes, int tolerance)
        {
            Console.WriteLine($"Tab: {tab}");
            Console.WriteLine($"Reference Image: {referenceImage}");
            Console.WriteLine($"Tolerance: {tolerance}");

            foreach (var compareType in compareTypes)
            {
                Console.WriteLine($"Compare Type: {compareType}");
            }

            // Load the input image and reference image
            Mat inputImage = Cv2.ImRead("input.png", ImreadModes.Color);
            Mat refImage = Cv2.ImRead(referenceImage, ImreadModes.Color);

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
    }
}
