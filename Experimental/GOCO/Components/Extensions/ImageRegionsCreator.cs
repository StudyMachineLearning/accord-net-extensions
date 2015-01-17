﻿using Accord.Extensions;
using Accord.Extensions.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GOCO
{
    public static class ImageRegionsCreatorExtensions
    {
        public static Func<Size, SizeF, float, Size> StepFunc = (imageSize, startSize, currentScaleFactor) =>
        {
            /*const float PYRAMID_SCALE_FACTOR = 0.25f;
            var shrinkFactor = 0.1f / 2;

            if (1f / PYRAMID_SCALE_FACTOR < currentScaleFactor) //if we entered to the 1st pyramid level
                shrinkFactor *= PYRAMID_SCALE_FACTOR;*/

            var stepX = (int)Math.Max(0.1f * currentScaleFactor * startSize.Width, 1);
            var stepY = (int)Math.Max(0.1f * currentScaleFactor * startSize.Height, 1);
            return new Size(stepX, stepY);
        };

        public static List<Rectangle> GenerateRegions(this Size areaSize, float startScale, float endScale, float widthHeightRatio = 1f, float scaleFactor = 1.2f)
        {
            return areaSize.GenerateRegions
                (
                  new SizeF(startScale * widthHeightRatio, startScale), 
                  new SizeF(endScale * widthHeightRatio, endScale), 
                  scaleFactor
                );
        }

        public static List<Rectangle> GenerateRegions(this Size areaSize, SizeF startSize, SizeF endSize, float scale = 1.2f)
        {
            return areaSize.GenerateRegions(startSize, endSize, scale, (scaleFactor) => StepFunc(areaSize, startSize, scaleFactor));
        }

        /// <summary>
        /// Generates windows to cover all image (e.g. can be used for object detection).
        /// </summary>
        /// <param name="areaSize">The area size to generate regions for.</param>
        /// <param name="startSize">Start window size. <seealso cref="scale"/>.</param>
        /// <param name="endSize">End window size. <seealso cref="scale"/>.</param>
        /// <param name="scale">
        /// Scale factor for window rescaling.
        /// <para>
        /// If the scale is less than 1:   every component of<see cref="StartSize"/> must be larger  than <see cref="EndSize"/>.
        /// If the scale is bigger than 1: every component of<see cref="StartSize"/> must be smaller than <see cref="EndSize"/>.
        /// IF the scale is 1:             every component of<see cref="StartSize"/> must be equal   to   <see cref="EndSize"/>.  
        /// </para>
        /// </param>
        /// <param name="stepFunc">
        /// Stepping function receives the image size, current scale factor (multiplies <see cref="startSize"/>). 
        /// Function outputs the steps in horizontal and vertical direction.
        /// </param>
        /// <returns> The collection of regions.</returns>
        public static List<Rectangle> GenerateRegions(this Size areaSize, SizeF startSize, SizeF endSize, float scale, Func<float, Size> stepFunc)
        {
            validateProperties(startSize, endSize, scale);
            return generateWindows(areaSize, startSize, endSize, scale, stepFunc);
        }

        private static List<Rectangle> generateWindows(Size imageSize, SizeF startSize, SizeF endSize, float scale, Func<float, Size> stepFunc)
        {
            var windows = new List<Rectangle>();
            Rectangle window = default(Rectangle);

            foreach (var factor in generateScales(imageSize, startSize, endSize, scale))
            {
                window.Width = (int)Math.Floor(startSize.Width * factor);
                window.Height = (int)Math.Floor(startSize.Height * factor);

                var step = stepFunc(factor);

                while (window.Bottom < imageSize.Height)
                {
                    while (window.Right < imageSize.Width)
                    {
                        windows.Add(window);

                        window.X += step.Width;
                    }

                    window.X = 0;
                    window.Y += step.Height;
                }

                window.Y = 0;
            }

            return windows;
        }

        private static IEnumerable<float> generateScales(Size imageSize, SizeF startSize, SizeF endSize, float scale)
        {
            var maxSize = new Size
            {
                Width = Math.Min(imageSize.Width, (int)endSize.Width),
                Height = Math.Min(imageSize.Height, (int)endSize.Height)
            };

            float start = 1f;
            float maxFactor = Math.Min(maxSize.Width / startSize.Width, maxSize.Height / startSize.Height);

            for (float f = start; f <= maxFactor; f *= scale)
                yield return f;
        }

        private static void validateProperties(SizeF startSize, SizeF endSize, float scale)
        {
            if (scale != 1 && startSize.Equals(endSize))
                throw new Exception("Scale should be different than 1, if the start size is not equal to destination size.");

            if (scale >= 1 &&
               ((startSize.Width > endSize.Width) || (startSize.Height > endSize.Height)))
            {
                throw new Exception("StartSize is bigger than EndSize, but the scale factor is bigger or equal to 1!");
            }

            if (scale <= 1 &&
               ((startSize.Width < endSize.Width) || (startSize.Height < endSize.Height)))
            {
                throw new Exception("StartSize is smaller than EndSize, but the scale factor is smaller or equal to 1!");
            }
        }

    }
}
