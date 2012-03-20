//----------------------------------------------------------------------------
//  Copyright (C) 2004-2012 by EMGU. All rights reserved.       
//----------------------------------------------------------------------------

﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.Util;

namespace Emgu.CV.Features2D
{
   /// <summary>
   /// Wrapped SIFT detector
   /// </summary>
   public class SIFTDetector : UnmanagedObject, IKeyPointDetector, IDescriptorExtractor<float>
   {
      /// <summary>
      /// Create a SIFTDetector using the specific values
      /// </summary>
      /// <param name="nFeatures">The desired number of features. Use 0 for un-restricted number of features</param>
      /// <param name="nOctaveLayers">The number of octave layers. Use 3 for default</param>
      /// <param name="contrastThreshold">Contrast threshold. Use 0.04 as default</param>
      /// <param name="edgeThreshold">Detector parameter. Use 10.0 as default</param>
      /// <param name="sigma">Use 1.6 as default</param>
      public SIFTDetector(
         int nFeatures, int nOctaveLayers,
         double contrastThreshold, double edgeThreshold,
         double sigma)
      {
         _ptr = CvInvoke.CvSIFTDetectorCreate(nFeatures, nOctaveLayers, contrastThreshold, edgeThreshold, sigma);
      }

      /// <summary>
      /// Create a SIFT detector with the default parameters
      /// </summary>
      public SIFTDetector()
         : this(0, 3, 0.04, 10, 1.6)
      {
      }

      /// <summary>
      /// Detect the SURF keypoints from the image
      /// </summary>
      /// <param name="image">The image to extract SURF features from</param>
      /// <param name="mask">The optional mask, can be null if not needed</param>
      /// <returns>An array of SURF key points</returns>
      public MKeyPoint[] DetectKeyPoints(Image<Gray, Byte> image, Image<Gray, byte> mask)
      {
         using (VectorOfKeyPoint kpts = DetectKeyPointsRaw(image, mask))
         {
            return kpts.ToArray();
         }
      }

      /// <summary>
      /// Detect image features from the given image
      /// </summary>
      /// <param name="image">The image to detect features from</param>
      /// <param name="mask">The optional mask, can be null if not needed</param>
      /// <returns>The Image features detected from the given image</returns>
      public ImageFeature<float>[] DetectFeatures(Image<Gray, Byte> image, Image<Gray, byte> mask)
      {
         using (VectorOfKeyPoint pts = DetectKeyPointsRaw(image, mask))
         using (Matrix<float> descVec = ComputeDescriptorsRaw(image, mask, pts))
         {
            return ImageFeature<float>.ConvertFromRaw(pts, descVec);
         }
      }

      /// <summary>
      /// Get the size of the descriptor
      /// </summary>
      public int DescriptorSize
      {
         get
         {
            return CvInvoke.CvSIFTDetectorGetDescriptorSize(_ptr);
         }
      }

      /*
      /// <summary>
      /// Compute the descriptor given the bgr image and the point location, using oppponent color (CGIV 2008 "Color Descriptors for Object Category Recognition").
      /// </summary>
      /// <param name="image">The image where the descriptor will be computed from</param>
      /// <param name="keyPoints">The keypoint where the descriptor will be computed from</param>
      /// <returns>The descriptors founded on the keypoint location</returns>
      public Matrix<float> ComputeDescriptorsRaw(Image<Bgr, Byte> image, VectorOfKeyPoint keyPoints)
      {
         int count = keyPoints.Size;
         if (count == 0) return null;
         Matrix<float> descriptors = new Matrix<float>(count, DescriptorSize * 3, 1);
         CvSIFTDetectorComputeDescriptorsBGR(_ptr, image, keyPoints, descriptors);
         return descriptors;
      }*/

      /// <summary>
      /// Compute the descriptor given the image and the point location
      /// </summary>
      /// <param name="image">The image where the descriptor will be computed from</param>
      /// <param name="mask">The optional mask, can be null if not needed</param>
      /// <param name="keyPoints">The keypoint where the descriptor will be computed from</param>
      /// <returns>The descriptors founded on the keypoint location</returns>
      public ImageFeature<float>[] ComputeDescriptors(Image<Gray, Byte> image, Image<Gray, byte> mask, MKeyPoint[] keyPoints)
      {
         if (keyPoints.Length == 0) return new ImageFeature<float>[0];
         using (VectorOfKeyPoint kpts = new VectorOfKeyPoint())
         {
            kpts.Push(keyPoints);
            using (Matrix<float> descriptor = ComputeDescriptorsRaw(image, mask, kpts))
            {
               return ImageFeature<float>.ConvertFromRaw(kpts, descriptor);
            }
         }
      }

      /// <summary>
      /// Release the unmanaged resources associated with this object
      /// </summary>
      protected override void DisposeObject()
      {
         CvInvoke.CvSIFTDetectorRelease(ref _ptr);
      }

      #region IKeyPointDetector Members
      /// <summary>
      /// Detect the SIFT keypoints from the image
      /// </summary>
      /// <param name="image">The image to extract SIFT features from</param>
      /// <param name="mask">The optional mask, can be null if not needed</param>
      /// <returns>An array of SIFT key points</returns>
      public VectorOfKeyPoint DetectKeyPointsRaw(Image<Gray, Byte> image, Image<Gray, byte> mask)
      {
         VectorOfKeyPoint kpts = new VectorOfKeyPoint();
         CvInvoke.CvFeatureDetectorDetectKeyPoints(_ptr, image, mask, kpts);
         return kpts;
      }

      /// <summary>
      /// Get the feature detector. 
      /// </summary>
      /// <returns>The feature detector</returns>
      IntPtr IKeyPointDetector.FeatureDetectorPtr
      {
         get
         {
            return _ptr;
         }
      }
      #endregion

      /// <summary>
      /// Compute the descriptor given the image and the point location
      /// </summary>
      /// <param name="image">The image where the descriptor will be computed from</param>
      /// <param name="mask">The optional mask, can be null if not needed</param>
      /// <param name="keyPoints">The keypoint where the descriptor will be computed from. Keypoints for which a descriptor cannot be computed are removed.</param>
      /// <returns>The descriptors founded on the keypoint location</returns>
      private Matrix<float> ComputeDescriptorsRawHelper(CvArray<Byte> image, Image<Gray, byte> mask, VectorOfKeyPoint keyPoints)
      {
         if (mask != null)
            keyPoints.FilterByPixelsMask(mask);

         int count = keyPoints.Size;
         if (count == 0) return null;
         Matrix<float> descriptors = new Matrix<float>(count, image.NumberOfChannels * DescriptorSize, 1);
         CvInvoke.CvSIFTDetectorComputeDescriptors(_ptr, image, keyPoints, descriptors);
         return descriptors;
      }

      /// <summary>
      /// Compute the descriptor given the image and the point location, using oppponent color (CGIV 2008 "Color Descriptors for Object Category Recognition").
      /// </summary>
      /// <param name="image">The image where the descriptor will be computed from</param>
      /// <param name="mask">The optional mask, can be null if not needed</param>
      /// <param name="keyPoints">The keypoint where the descriptor will be computed from. Keypoints for which a descriptor cannot be computed are removed.</param>
      /// <returns>The descriptors founded on the keypoint location</returns>
      public Matrix<float> ComputeDescriptorsRaw(Image<Bgr, Byte> image, Image<Gray, Byte> mask, VectorOfKeyPoint keyPoints)
      {
         return ComputeDescriptorsRawHelper(image, mask, keyPoints);
      }

      #region IDescriptorExtractor<float>
      /// <summary>
      /// Compute the descriptor given the image and the point location
      /// </summary>
      /// <param name="image">The image where the descriptor will be computed from</param>
      /// <param name="mask">The optional mask, can be null if not needed</param>
      /// <param name="keyPoints">The keypoint where the descriptor will be computed from. Keypoints for which a descriptor cannot be computed are removed.</param>
      /// <returns>The descriptors founded on the keypoint location</returns>
      public Matrix<float> ComputeDescriptorsRaw(Image<Gray, Byte> image, Image<Gray, byte> mask, VectorOfKeyPoint keyPoints)
      {
         return ComputeDescriptorsRawHelper(image, mask, keyPoints);
      }

      IntPtr IDescriptorExtractor<float>.DescriptorExtratorPtr
      {
         get { return _ptr; }
      }

      #endregion
   }
}

namespace Emgu.CV
{
   public static partial class CvInvoke
   {
      /*
      [DllImport(CvInvoke.EXTERN_LIBRARY, CallingConvention = CvInvoke.CvCallingConvention)]
      private extern static void CvSIFTDetectorDetectFeature(
         IntPtr detector,
         IntPtr image,
         IntPtr mask,
         IntPtr keypoints,
         IntPtr descriptors);*/

      [DllImport(CvInvoke.EXTERN_LIBRARY, CallingConvention = CvInvoke.CvCallingConvention)]
      internal extern static int CvSIFTDetectorGetDescriptorSize(IntPtr detector);

      [DllImport(CvInvoke.EXTERN_LIBRARY, CallingConvention = CvInvoke.CvCallingConvention)]
      internal extern static void CvSIFTDetectorComputeDescriptors(
         IntPtr detector,
         IntPtr image,
         IntPtr keypoints,
         IntPtr descriptors);

      [DllImport(CvInvoke.EXTERN_LIBRARY, CallingConvention = CvInvoke.CvCallingConvention)]
      internal extern static IntPtr CvSIFTDetectorCreate(
         int nFeatures, int nOctaveLayers,
         double contrastThreshold, double edgeThreshold,
         double sigma);

      [DllImport(CvInvoke.EXTERN_LIBRARY, CallingConvention = CvInvoke.CvCallingConvention)]
      internal extern static void CvSIFTDetectorRelease(ref IntPtr detector);
   }
}