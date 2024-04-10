using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace testengine.module.sample
{
    public class SampleFunction : ReflectionFunction
    {
        public SampleFunction() : base("Sample", FormulaType.Blank)
        {
        }

        public BlankValue Execute()
        {
            Console.WriteLine("!!! SAMPLE !!!");
            return BlankValue.NewBlank();
        }
    }
}
