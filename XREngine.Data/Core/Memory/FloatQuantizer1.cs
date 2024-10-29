namespace XREngine.Data
{
    public static class FloatQuantizer
    {
        /// <summary>
        /// Calculates the quantization parameters given the minimum value, maximum value, and maximum allowable error.
        /// This method allocates no memory.
        /// </summary>
        public static void CalculateQuantizationParameters(
            float minValue, float maxValue, float maxError,
            out float quantizationStepSize, out int numberOfLevels, out int numberOfBits)
        {
            if (maxError <= 0)
                throw new ArgumentException("maxError must be greater than zero.");

            float range = maxValue - minValue;

            // Calculate quantization parameters
            quantizationStepSize = 2 * maxError;
            numberOfLevels = (int)Math.Ceiling(range / quantizationStepSize) + 1;
            numberOfBits = (int)Math.Ceiling(Math.Log(numberOfLevels, 2));

            // Ensure the number of bits is at least 1
            numberOfBits = Math.Max(1, numberOfBits);
        }

        /// <summary>
        /// Quantizes a float value to its integer representation without allocations.
        /// </summary>
        public static int Quantize(
            float value, float minValue, float maxValue,
            float quantizationStepSize, int numberOfLevels)
        {
            // Clamp value to the min and max range
            value = Math.Max(minValue, Math.Min(maxValue, value));

            // Quantize the value
            int quantizedValue = (int)Math.Round((value - minValue) / quantizationStepSize);

            // Clamp quantized value to valid range
            quantizedValue = Math.Max(0, Math.Min(numberOfLevels - 1, quantizedValue));

            return quantizedValue;
        }

        /// <summary>
        /// Dequantizes an integer representation back to a float value without allocations.
        /// </summary>
        public static float Dequantize(
            int quantizedValue, float minValue,
            float quantizationStepSize, int numberOfLevels)
        {
            // Clamp quantized value to valid range
            quantizedValue = Math.Max(0, Math.Min(numberOfLevels - 1, quantizedValue));

            // Dequantize the value
            float dequantizedValue = quantizedValue * quantizationStepSize + minValue;

            return dequantizedValue;
        }

        /// <summary>
        /// Quantizes an array of float values in-place without allocations.
        /// </summary>
        public static void QuantizeArray(
            ReadOnlySpan<float> values, Span<int> quantizedValues,
            float minValue, float maxValue,
            float quantizationStepSize, int numberOfLevels)
        {
            if (values.Length != quantizedValues.Length)
                throw new ArgumentException("Input and output spans must have the same length.");

            for (int i = 0; i < values.Length; i++)
            {
                quantizedValues[i] = Quantize(
                    values[i], minValue, maxValue, quantizationStepSize, numberOfLevels);
            }
        }

        /// <summary>
        /// Dequantizes an array of integer representations back to float values in-place without allocations.
        /// </summary>
        public static void DequantizeArray(
            ReadOnlySpan<int> quantizedValues, Span<float> dequantizedValues,
            float minValue, float quantizationStepSize, int numberOfLevels)
        {
            if (quantizedValues.Length != dequantizedValues.Length)
                throw new ArgumentException("Input and output spans must have the same length.");

            for (int i = 0; i < quantizedValues.Length; i++)
            {
                dequantizedValues[i] = Dequantize(
                    quantizedValues[i], minValue, quantizationStepSize, numberOfLevels);
            }
        }

        /// <summary>
        /// Finds the minimum and maximum values in a span without allocations.
        /// </summary>
        public static void FindMinMax(
            ReadOnlySpan<float> values, out float minValue, out float maxValue)
        {
            if (values.Length == 0)
                throw new ArgumentException("values span cannot be empty.");

            minValue = values[0];
            maxValue = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] < minValue)
                    minValue = values[i];
                if (values[i] > maxValue)
                    maxValue = values[i];
            }
        }

        public static void QuantizeArray(ReadOnlySpan<float> values, Span<int> quantizedValues, float maxError = 0.01f)
        {
            FindMinMax(values, out float min, out float max);
            CalculateQuantizationParameters(min, max, maxError, out float step, out int levels, out int bits);
            QuantizeArray(values, quantizedValues, min, max, step, levels);
        }

        /// <summary>
        /// Quantizes an array of float values directly into a byte array without allocations.
        /// </summary>
        public static byte[] QuantizeToByteArray(ReadOnlySpan<float> values, out int bits, float maxError = 0.01f)
        {
            FindMinMax(values, out float min, out float max);
            CalculateQuantizationParameters(min, max, maxError, out float step, out int levels, out bits);
            int totalBytes = GetRequiredBytes(values.Length, bits);

            Span<byte> byteArray = new byte[totalBytes];
            int bitPosition = 0;

            for (int i = 0; i < values.Length; i++)
            {
                WriteBits(byteArray, bitPosition, QuantizeValue(values[i], min, max, step, levels), bits);
                bitPosition += bits;
            }

            return byteArray.ToArray();
        }

        /// <summary>
        /// Quantizes an array of float values directly into a byte array without allocations.
        /// </summary>
        public static void QuantizeToByteArray(
            ReadOnlySpan<float> values, Span<byte> byteArray,
            float minValue, float maxValue,
            float quantizationStepSize, int numberOfLevels, int numberOfBits)
        {
            int totalBytes = GetRequiredBytes(values.Length, numberOfBits);

            if (byteArray.Length < totalBytes)
                throw new ArgumentException("The byteArray is too small to hold the quantized data.");

            int bitPosition = 0;
            for (int i = 0; i < values.Length; i++)
            {
                WriteBits(byteArray, bitPosition, QuantizeValue(values[i], minValue, maxValue, quantizationStepSize, numberOfLevels), numberOfBits);
                bitPosition += numberOfBits;
            }
        }

        public static int GetRequiredBytes(int valueCount, int numberOfBits)
        {
            int totalBits = valueCount * numberOfBits;
            int totalBytes = (totalBits + 7) / 8; // Round up to the nearest whole byte
            return totalBytes;
        }

        /// <summary>
        /// Dequantizes float values directly from a byte array without allocations.
        /// </summary>
        public static void DequantizeFromByteArray(
            ReadOnlySpan<byte> input, Span<float> output,
            float minValue, float quantizationStepSize, int numberOfLevels, int numberOfBits)
        {
            int totalBits = output.Length * numberOfBits;
            int totalBytes = (totalBits + 7) / 8; // Round up to the nearest whole byte

            if (input.Length < totalBytes)
                throw new ArgumentException("The byteArray does not contain enough data.");

            int bitPosition = 0;
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = DequantizeValue(ReadBits(input, bitPosition, numberOfBits), minValue, quantizationStepSize, numberOfLevels);
                bitPosition += numberOfBits;
            }
        }

        /// <summary>
        /// Quantizes a single float value to its integer representation.
        /// </summary>
        private static int QuantizeValue(
            float value, float minValue, float maxValue,
            float quantizationStepSize, int numberOfLevels)
            => Math.Max(0, Math.Min(numberOfLevels - 1, (int)Math.Round((Math.Max(minValue, Math.Min(maxValue, value)) - minValue) / quantizationStepSize)));

        /// <summary>
        /// Dequantizes an integer representation back to a float value.
        /// </summary>
        private static float DequantizeValue(
            int quantizedValue, float minValue,
            float step, int levels)
            => Math.Max(0, Math.Min(levels - 1, quantizedValue)) * step + minValue;

        /// <summary>
        /// Writes a specified number of bits to a byte array at a given bit position.
        /// </summary>
        private static void WriteBits(Span<byte> byteArray, int bitPosition, int value, int bitsToWrite)
        {
            int byteIndex = bitPosition / 8;
            int bitOffset = bitPosition % 8;

            int bitsWritten = 0;

            while (bitsToWrite > 0)
            {
                int bitsAvailable = 8 - bitOffset;
                int bitsThisRound = Math.Min(bitsAvailable, bitsToWrite);

                // Prepare mask and value
                int mask = (1 << bitsThisRound) - 1;
                int valueToWrite = (value >> bitsWritten) & mask;

                // Clear the bits at the position
                byteArray[byteIndex] &= (byte)~(mask << bitOffset);

                // Set the bits
                byteArray[byteIndex] |= (byte)(valueToWrite << bitOffset);

                bitsWritten += bitsThisRound;
                bitsToWrite -= bitsThisRound;
                bitPosition += bitsThisRound;

                byteIndex = bitPosition / 8;
                bitOffset = bitPosition % 8;
            }
        }

        /// <summary>
        /// Reads a specified number of bits from a byte array at a given bit position.
        /// </summary>
        private static int ReadBits(ReadOnlySpan<byte> byteArray, int bitPosition, int bitsToRead)
        {
            int byteIndex = bitPosition / 8;
            int bitOffset = bitPosition % 8;

            int bitsRead = 0;
            int value = 0;
            int bitsCollected = 0;

            while (bitsToRead > 0)
            {
                int bitsAvailable = 8 - bitOffset;
                int bitsThisRound = Math.Min(bitsAvailable, bitsToRead);

                // Prepare mask
                int mask = (1 << bitsThisRound) - 1;
                int bits = (byteArray[byteIndex] >> bitOffset) & mask;

                value |= bits << bitsCollected;

                bitsCollected += bitsThisRound;
                bitsRead += bitsThisRound;
                bitsToRead -= bitsThisRound;
                bitPosition += bitsThisRound;

                byteIndex = bitPosition / 8;
                bitOffset = bitPosition % 8;
            }

            return value;
        }
    }
}