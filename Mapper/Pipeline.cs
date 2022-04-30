using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    class Pipeline {
        public float NormalizeMin { get; set; }
        public float NormalizeMax { get; set; }
        public bool ApplyWaterOffset { get; set; }
        public float WaterOffset { get; set; }

        public async Task Process(ProgressWindow progressWindow, ImageGroup<float> inputGroup, ImageGroup<ushort> outputGroup) {
            List<Task> tasks = new List<Task>();
            foreach (var tilePoint in outputGroup) {
                var input = inputGroup[tilePoint];
                var output = outputGroup[tilePoint];

                await TileHelper.ProcessImageParallel(output, (int batchID, int start, int end) => {
                    for (int i = start; i < end; i++) {
                        var point = output.GetPoint(i);
                        Process(input, output, point);
                    }
                });

                progressWindow.Increment();
            }
        }

        void Process(Image<float> input, Image<ushort> output, PointInt pos) {
            float data = input[pos];
            data = NormalizeHeight(data);

            if (ApplyWaterOffset) {
                data = GetWaterOffset(data);
            }

            output[pos] = ConvertToInteger(data);
        }

        float NormalizeHeight(float data) {
            return (float)Utility.InverseLerp(NormalizeMin, NormalizeMax, data);
        }

        float GetWaterOffset(float data) {
            return data;
        }

        ushort ConvertToInteger(float data) {
            return (ushort)(65535 * Utility.Clamp(data, 0, 1));
        }
    }
}
