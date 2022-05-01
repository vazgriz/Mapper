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

        float waterOffsetNormalized;

        public int GetProcessSteps(int tileCount) {
            return tileCount * tileCount;
        }

        public async Task Process(ProgressWindow progressWindow, ImageGroup<float> heightInput, ImageGroup<float> waterInput, ImageGroup<ushort> outputGroup) {
            float heightDifference = NormalizeMax - NormalizeMin;

            if (heightDifference != 0) {
                waterOffsetNormalized = WaterOffset / heightDifference;
            }

            List<Task> tasks = new List<Task>();
            foreach (var tilePoint in outputGroup) {
                var height = heightInput[tilePoint];
                var output = outputGroup[tilePoint];

                Image<float> water = null;
                if (ApplyWaterOffset) {
                    water = waterInput[tilePoint];
                }

                await TileHelper.ProcessImageParallel(output, (int batchID, int start, int end) => {
                    for (int i = start; i < end; i++) {
                        var point = output.GetPoint(i);
                        Process(height, water, output, point);

                        if (i % TileHelper.cancellationCheckInterval == 0) {
                            progressWindow.CancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                });

                progressWindow.Increment();
            }
        }

        void Process(Image<float> height, Image<float> water, Image<ushort> output, PointInt pos) {
            float data = height[pos];
            data = NormalizeHeight(data);

            if (ApplyWaterOffset) {
                data = GetWaterOffset(pos, water, data);
            }

            output[pos] = ConvertToInteger(data);
        }

        float NormalizeHeight(float data) {
            return (float)Utility.Clamp(Utility.InverseLerp(NormalizeMin, NormalizeMax, data), 0, 1);
        }

        float GetWaterOffset(PointInt pos, Image<float> water, float data) {
            float waterOffset = waterOffsetNormalized * water[pos];
            return Math.Max(0, data + waterOffset);
        }

        ushort ConvertToInteger(float data) {
            return (ushort)(65535 * Utility.Clamp(data, 0, 1));
        }
    }
}
