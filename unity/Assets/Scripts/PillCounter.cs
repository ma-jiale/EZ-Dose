using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace EZDose.PillCounter
{
    /// <summary>
    /// 药片计数核心算法类
    /// 使用OpenCV进行图像处理和轮廓分析
    /// </summary>
    public class PillCounter
    {
        // 背景相关
        private Mat background;
        private bool backgroundCaptured;
        
        // 边缘检测参数
        private readonly int edgeThreshold = 1000;
        private readonly Queue<int> recentEdgeCounts = new Queue<int>(10);
        private readonly int stableFramesNeeded = 15;
        private int stableCount = 0;
        
        // Focus detection parameters (stability-based, for low-texture surfaces)
        // Instead of requiring a minimum focus value, we detect when focus STOPS changing
        private readonly double focusStabilityThreshold = 0.5; // Max coefficient of variation for stable focus
        private readonly Queue<double> recentFocusScores = new Queue<double>(10);
        private readonly int stableFocusFramesNeeded = 10;
        private int stableFocusCount = 0;
        
        // 画面裁切参数
        private readonly int cropMargin = 50;
        
        // 形态学操作参数
        private readonly Mat morphKernel;
        
        // 轮廓过滤参数
        private readonly double minContourArea = 50;
        private readonly double maxContourArea = 100000;
        private readonly double convexityThreshold = 0.90;
        
        // 形状分析参数
        private readonly double aspectRatioThreshold = 3.0;
        private readonly double solidityThreshold = 0.85;
        
        public bool IsBackgroundCaptured => backgroundCaptured;
        
        public PillCounter()
        {
            morphKernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));
        }
        
        /// <summary>
        /// 裁切画面，去除边缘杂物
        /// </summary>
        private Mat CropFrame(Mat frame)
        {
            int h = frame.rows();
            int w = frame.cols();
            OpenCVForUnity.CoreModule.Rect roi = new OpenCVForUnity.CoreModule.Rect(
                cropMargin, cropMargin,
                w - 2 * cropMargin, h - 2 * cropMargin
            );
            return new Mat(frame, roi);
        }
        
        /// <summary>
        /// 检测图像中的边缘
        /// </summary>
        public (int edgeCount, Mat edges) DetectEdges(Mat frame)
        {
            using (Mat cropped = CropFrame(frame))
            using (Mat gray = new Mat())
            using (Mat blurred = new Mat())
            using (Mat edges = new Mat())
            {
                Imgproc.cvtColor(cropped, gray, Imgproc.COLOR_BGR2GRAY);
                Imgproc.GaussianBlur(gray, blurred, new Size(5, 5), 0);
                Imgproc.Canny(blurred, edges, 50, 150);
                
                int edgeCount = Core.countNonZero(edges);
                return (edgeCount, edges.clone());
            }
        }
        
        /// <summary>
        /// Calculate focus quality using Laplacian variance method.
        /// Higher variance indicates sharper/more focused image.
        /// </summary>
        public double CheckFocusQuality(Mat frame)
        {
            using (Mat cropped = CropFrame(frame))
            using (Mat gray = new Mat())
            using (Mat laplacian = new Mat())
            {
                Imgproc.cvtColor(cropped, gray, Imgproc.COLOR_BGR2GRAY);
                Imgproc.Laplacian(gray, laplacian, CvType.CV_64F);
                
                // Calculate variance of Laplacian
                MatOfDouble mean = new MatOfDouble();
                MatOfDouble stddev = new MatOfDouble();
                Core.meanStdDev(laplacian, mean, stddev);
                
                double variance = Math.Pow(stddev.toArray()[0], 2);
                
                mean.Dispose();
                stddev.Dispose();
                
                return variance;
            }
        }
        
        /// <summary>
        /// Check if focus is stable (not changing) for required number of frames.
        /// Works for low-texture surfaces where absolute focus values are low.
        /// Uses coefficient of variation to detect stability regardless of absolute value.
        /// </summary>
        public bool IsFocusStable(double focusScore)
        {
            recentFocusScores.Enqueue(focusScore);
            if (recentFocusScores.Count > 10)
                recentFocusScores.Dequeue();
            
            if (recentFocusScores.Count < 10)
                return false;
            
            // Calculate coefficient of variation (stddev/mean) to detect stability
            // This works regardless of whether absolute values are high or low
            double mean = recentFocusScores.Average();
            if (mean < 0.1) mean = 0.1; // Avoid division by near-zero
            
            double variance = recentFocusScores.Average(s => Math.Pow(s - mean, 2));
            double stddev = Math.Sqrt(variance);
            double coefficientOfVariation = stddev / mean;
            
            // Focus is stable when coefficient of variation is low (values not fluctuating much)
            bool isStable = coefficientOfVariation < focusStabilityThreshold;
            
            if (isStable)
            {
                stableFocusCount++;
                return stableFocusCount >= stableFocusFramesNeeded;
            }
            else
            {
                stableFocusCount = 0;
                return false;
            }
        }
        
        /// <summary>
        /// 判断场景是否稳定（适合作为背景）
        /// Now requires both edge stability AND focus stability.
        /// </summary>
        public bool IsSceneStable(int edgeCount, double focusScore)
        {
            // Check focus stability first
            bool focusStable = IsFocusStable(focusScore);
            
            recentEdgeCounts.Enqueue(edgeCount);
            if (recentEdgeCounts.Count > 10)
                recentEdgeCounts.Dequeue();
            
            if (recentEdgeCounts.Count < 10)
                return false;
            
            // 计算方差和均值
            double mean = recentEdgeCounts.Average();
            double variance = recentEdgeCounts.Average(e => Math.Pow(e - mean, 2));
            
            // 场景稳定判断 - now requires BOTH edge stability AND focus stability
            bool edgeStable = variance < 8000 && mean < edgeThreshold;
            
            if (edgeStable && focusStable)
            {
                stableCount++;
                return stableCount >= stableFramesNeeded;
            }
            else
            {
                stableCount = 0;
                return false;
            }
        }
        
        /// <summary>
        /// Get current focus stability threshold for debugging.
        /// </summary>
        public double GetFocusThreshold() => focusStabilityThreshold;
        
        /// <summary>
        /// 捕捉背景图像
        /// </summary>
        public void CaptureBackground(Mat frame)
        {
            try
            {
                using (Mat cropped = CropFrame(frame))
                using (Mat gray = new Mat())
                {
                    Imgproc.cvtColor(cropped, gray, Imgproc.COLOR_BGR2GRAY);
                    background = new Mat();
                    Imgproc.GaussianBlur(gray, background, new Size(5, 5), 0);
                    backgroundCaptured = true;
                    Debug.Log("背景已捕捉");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"捕捉背景失败: {e.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 重置背景捕捉状态
        /// </summary>
        public void ResetBackground()
        {
            backgroundCaptured = false;
            stableCount = 0;
            stableFocusCount = 0;
            recentEdgeCounts.Clear();
            recentFocusScores.Clear();
            if (background != null)
            {
                background.Dispose();
                background = null;
            }
            Debug.Log("背景已重置");
        }
        
        /// <summary>
        /// 图像预处理：背景减法、二值化和形态学操作
        /// </summary>
        private Mat PreprocessImage(Mat frame)
        {
            using (Mat cropped = CropFrame(frame))
            using (Mat gray = new Mat())
            using (Mat blurred = new Mat())
            using (Mat diff = new Mat())
            using (Mat binary = new Mat())
            {
                Imgproc.cvtColor(cropped, gray, Imgproc.COLOR_BGR2GRAY);
                Imgproc.GaussianBlur(gray, blurred, new Size(5, 5), 0);
                
                // 背景减法
                Core.absdiff(background, blurred, diff);
                
                // 二值化
                Imgproc.threshold(diff, binary, 40, 255, Imgproc.THRESH_BINARY);
                
                // 形态学开运算去噪
                Imgproc.morphologyEx(binary, binary, Imgproc.MORPH_OPEN, morphKernel);
                
                // 腐蚀操作：分离轻微相连的轮廓
                using (Mat erosionKernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(6, 6)))
                {
                    Imgproc.erode(binary, binary, erosionKernel, new Point(-1, -1), 2);
                }
                
                return binary.clone();
            }
        }
        
        /// <summary>
        /// 分析轮廓的形状特征
        /// </summary>
        private ContourFeatures AnalyzeContourShape(MatOfPoint contour)
        {
            double area = Imgproc.contourArea(contour);
            
            // 计算边界矩形
            OpenCVForUnity.CoreModule.Rect rect = Imgproc.boundingRect(contour);
            double aspectRatio = Math.Max(rect.width, rect.height) / 
                                (double)Math.Min(rect.width, rect.height);
            
            // 计算凸包
            MatOfInt hull = new MatOfInt();
            Imgproc.convexHull(contour, hull);
            
            // 转换凸包点
            MatOfPoint hullPoints = new MatOfPoint();
            List<Point> hullPointsList = new List<Point>();
            int[] hullIndices = hull.toArray();
            Point[] contourPoints = contour.toArray();
            
            foreach (int idx in hullIndices)
            {
                if (idx < contourPoints.Length)
                    hullPointsList.Add(contourPoints[idx]);
            }
            hullPoints.fromList(hullPointsList);
            
            double hullArea = Imgproc.contourArea(hullPoints);
            double convexity = hullArea > 0 ? area / hullArea : 0;
            double solidity = convexity; // 实心度等于凸包度
            
            // 计算周长
            double perimeter = Imgproc.arcLength(new MatOfPoint2f(contour.toArray()), true);
            
            // 计算圆形度
            double circularity = perimeter > 0 ? 
                4 * Math.PI * area / (perimeter * perimeter) : 0;
            
            hull.Dispose();
            hullPoints.Dispose();
            
            return new ContourFeatures
            {
                Area = area,
                AspectRatio = aspectRatio,
                Convexity = convexity,
                Solidity = solidity,
                Circularity = circularity,
                Perimeter = perimeter
            };
        }
        
        /// <summary>
        /// 判断轮廓是否为单个药片
        /// </summary>
        private bool IsSinglePill(MatOfPoint contour)
        {
            var features = AnalyzeContourShape(contour);
            
            // 面积过滤
            if (features.Area < minContourArea || features.Area > maxContourArea)
                return false;
            
            // 多重判断条件
            bool isConvex = features.Convexity >= convexityThreshold;
            bool isSolid = features.Solidity >= solidityThreshold;
            bool isReasonableRatio = features.AspectRatio <= aspectRatioThreshold;
            bool isCircularEnough = features.Circularity > 0.3;
            
            return isConvex && isSolid && isReasonableRatio && isCircularEnough;
        }
        
        /// <summary>
        /// 基于面积检测多个药片
        /// </summary>
        private int DetectMultiplePillsByArea(MatOfPoint contour, double referenceArea)
        {
            if (referenceArea == 0)
                return 1;
            
            double contourArea = Imgproc.contourArea(contour);
            double ratio = contourArea / referenceArea;
            
            // 精确估算
            if (ratio < 0.7) return 0;
            else if (ratio <= 1.2) return 1;
            else if (ratio <= 2.4) return 2;
            else if (ratio <= 3.6) return 3;
            else if (ratio <= 4.8) return 4;
            else return Math.Max(1, (int)Math.Round(ratio));
        }
        
        /// <summary>
        /// 计算参考药片面积（使用中位数）
        /// </summary>
        private double CalculateReferenceArea(List<MatOfPoint> singlePillContours)
        {
            if (singlePillContours.Count == 0)
                return 0;
            
            var areas = singlePillContours
                .Select(c => Imgproc.contourArea(c))
                .OrderBy(a => a)
                .ToList();
            
            // 使用中位数
            double medianArea = areas[areas.Count / 2];
            
            // 过滤异常值
            var filteredAreas = areas
                .Where(a => a >= 0.6 * medianArea && a <= 1.4 * medianArea)
                .ToList();
            
            if (filteredAreas.Count > 0)
                return filteredAreas[filteredAreas.Count / 2];
            else
                return medianArea;
        }
        
        /// <summary>
        /// 额外的轮廓分离处理
        /// </summary>
        private Mat SeparateContours(Mat binary)
        {
            using (Mat separationKernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3)))
            using (Mat eroded = new Mat())
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                Imgproc.erode(binary, eroded, separationKernel, new Point(-1, -1), 2);
                
                // 连通组件分析
                int numLabels = Imgproc.connectedComponentsWithStats(eroded, labels, stats, centroids);
                
                Mat separated = Mat.zeros(binary.size(), binary.type());
                
                using (Mat recoveryKernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(4, 4)))
                {
                    for (int i = 1; i < numLabels; i++)
                    {
                        // 提取单个连通组件
                        Mat componentMask = new Mat();
                        Core.compare(labels, new Scalar(i), componentMask, Core.CMP_EQ);
                        
                        // 膨胀恢复
                        Mat recovered = new Mat();
                        Imgproc.dilate(componentMask, recovered, recoveryKernel, new Point(-1, -1), 2);
                        
                        // 合并
                        Core.bitwise_or(separated, recovered, separated);
                        
                        componentMask.Dispose();
                        recovered.Dispose();
                    }
                }
                
                return separated;
            }
        }
        
        /// <summary>
        /// 主要的药片计数函数
        /// </summary>
        public (int pillCount, Mat resultFrame, CountingDebugInfo debugInfo) CountPills(Mat frame)
        {
            if (!backgroundCaptured)
            {
                return (0, frame.clone(), new CountingDebugInfo());
            }
            
            try
            {
                // 预处理图像
                using (Mat binary = PreprocessImage(frame))
                using (Mat processedBinary = SeparateContours(binary))
                {
                    // 查找轮廓
                    List<MatOfPoint> contours = new List<MatOfPoint>();
                    Mat hierarchy = new Mat();
                    Imgproc.findContours(processedBinary, contours, hierarchy, 
                        Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
                    hierarchy.Dispose();
                    
                    // 过滤轮廓
                    var validContours = contours
                        .Where(c => {
                            double area = Imgproc.contourArea(c);
                            return area >= minContourArea && area <= maxContourArea;
                        })
                        .ToList();
                    
                    if (validContours.Count == 0)
                    {
                        foreach (var c in contours) c.Dispose();
                        return (0, frame.clone(), new CountingDebugInfo());
                    }
                    
                    // 分类轮廓
                    var singlePillContours = new List<MatOfPoint>();
                    var multiplePillContours = new List<MatOfPoint>();
                    
                    foreach (var contour in validContours)
                    {
                        if (IsSinglePill(contour))
                            singlePillContours.Add(contour);
                        else
                            multiplePillContours.Add(contour);
                    }
                    
                    // 计算参考面积
                    double referenceArea = CalculateReferenceArea(singlePillContours);
                    
                    // 重新分类（面积过大的单个药片）
                    var reclassifiedContours = new List<MatOfPoint>();
                    if (referenceArea > 0)
                    {
                        double areaThreshold = referenceArea * 1.2;
                        var finalSingleContours = new List<MatOfPoint>();
                        
                        foreach (var contour in singlePillContours)
                        {
                            double contourArea = Imgproc.contourArea(contour);
                            if (contourArea > areaThreshold)
                            {
                                multiplePillContours.Add(contour);
                                reclassifiedContours.Add(contour);
                            }
                            else
                            {
                                finalSingleContours.Add(contour);
                            }
                        }
                        
                        singlePillContours = finalSingleContours;
                        
                        // 重新计算参考面积
                        if (singlePillContours.Count > 0)
                            referenceArea = CalculateReferenceArea(singlePillContours);
                    }
                    
                    // 计算总药片数量
                    int totalPills = singlePillContours.Count;
                    
                    foreach (var contour in multiplePillContours)
                    {
                        int estimatedPills = DetectMultiplePillsByArea(contour, referenceArea);
                        totalPills += estimatedPills;
                    }
                    
                    // 绘制结果
                    Mat resultFrame = DrawResults(frame, singlePillContours, 
                        multiplePillContours, reclassifiedContours, totalPills, referenceArea);
                    
                    var debugInfo = new CountingDebugInfo
                    {
                        SinglePillCount = singlePillContours.Count,
                        MultiplePillCount = multiplePillContours.Count,
                        ReclassifiedCount = reclassifiedContours.Count,
                        ReferenceArea = referenceArea
                    };
                    
                    // 清理
                    foreach (var c in contours) c.Dispose();
                    
                    return (totalPills, resultFrame, debugInfo);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"计数失败: {e.Message}");
                return (0, frame.clone(), new CountingDebugInfo());
            }
        }
        
        /// <summary>
        /// Try to detect a single pill for calibration purposes.
        /// Returns success and detected pixel area if exactly one pill is found.
        /// Uses pre-erosion area for more accurate calibration since single pills are used.
        /// </summary>
        /// <param name="frame">Camera frame to analyze</param>
        /// <returns>Tuple of (success, pixelArea) - success is true if exactly 1 pill detected</returns>
        public (bool success, float pixelArea, string message) TryCalibrateSinglePill(Mat frame)
        {
            if (!backgroundCaptured)
            {
                return (false, 0f, "请先捕捉背景");
            }
            
            try
            {
                // First use standard counting to verify exactly 1 pill is present
                var (pillCount, resultFrame, debugInfo) = CountPills(frame);
                resultFrame.Dispose();
                
                if (pillCount == 0)
                {
                    return (false, 0f, "未检测到药片，请放置一颗药片");
                }
                
                if (pillCount > 1)
                {
                    return (false, 0f, $"检测到{pillCount}颗药片，请只放置一颗");
                }
                
                // Exactly one pill found - now calculate pre-erosion area for calibration
                float preErosionArea = GetPreErosionPillArea(frame);
                if (preErosionArea <= 0)
                {
                    return (false, 0f, "无法获取药片面积");
                }
                
                Debug.Log($"[PillCounter] Single pill calibration: pre-erosion area = {preErosionArea:.1f} pixels (post-erosion was {debugInfo.ReferenceArea:.1f})");
                return (true, preErosionArea, $"检测成功: {preErosionArea:.1f} 像素");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PillCounter] Calibration failed: {e.Message}");
                return (false, 0f, $"校准失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get the pre-erosion area of a single pill for calibration.
        /// Uses preprocessing without erosion to get accurate pill size.
        /// </summary>
        private float GetPreErosionPillArea(Mat frame)
        {
            using (Mat cropped = CropFrame(frame))
            using (Mat gray = new Mat())
            using (Mat blurred = new Mat())
            using (Mat diff = new Mat())
            using (Mat binary = new Mat())
            {
                Imgproc.cvtColor(cropped, gray, Imgproc.COLOR_BGR2GRAY);
                Imgproc.GaussianBlur(gray, blurred, new Size(5, 5), 0);
                
                // Background subtraction
                Core.absdiff(background, blurred, diff);
                
                // Binarization
                Imgproc.threshold(diff, binary, 40, 255, Imgproc.THRESH_BINARY);
                
                // Morphological opening to remove noise (NO erosion for calibration)
                Imgproc.morphologyEx(binary, binary, Imgproc.MORPH_OPEN, morphKernel);
                
                // Find contours
                List<MatOfPoint> contours = new List<MatOfPoint>();
                Mat hierarchy = new Mat();
                Imgproc.findContours(binary, contours, hierarchy, 
                    Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
                hierarchy.Dispose();
                
                // Filter and find the largest valid contour (should be the single pill)
                double maxArea = 0;
                foreach (var contour in contours)
                {
                    double area = Imgproc.contourArea(contour);
                    if (area >= minContourArea && area <= maxContourArea && area > maxArea)
                    {
                        maxArea = area;
                    }
                }
                
                // Cleanup
                foreach (var c in contours) c.Dispose();
                
                return (float)maxArea;
            }
        }
        
        /// <summary>
        /// 绘制检测结果
        /// </summary>
        private Mat DrawResults(Mat frame, List<MatOfPoint> singleContours, 
            List<MatOfPoint> multipleContours, List<MatOfPoint> reclassifiedContours,
            int totalPills, double referenceArea)
        {
            Mat result = frame.clone();
            int h = frame.rows();
            int w = frame.cols();
            
            // 绘制裁切区域边界
            Imgproc.rectangle(result, 
                new Point(cropMargin, cropMargin),
                new Point(w - cropMargin, h - cropMargin),
                new Scalar(255, 255, 0), 2);
            
            // 调整轮廓坐标（考虑裁切偏移）
            Point offset = new Point(cropMargin, cropMargin);
            
            // 绘制轮廓
            foreach (var contour in singleContours)
            {
                var offsetContour = OffsetContour(contour, offset);
                Imgproc.drawContours(result, new List<MatOfPoint> { offsetContour }, 
                    -1, new Scalar(0, 255, 0), 2);
                offsetContour.Dispose();
            }
            
            foreach (var contour in multipleContours)
            {
                var offsetContour = OffsetContour(contour, offset);
                Imgproc.drawContours(result, new List<MatOfPoint> { offsetContour }, 
                    -1, new Scalar(0, 0, 255), 2);
                offsetContour.Dispose();
            }
            
            foreach (var contour in reclassifiedContours)
            {
                var offsetContour = OffsetContour(contour, offset);
                Imgproc.drawContours(result, new List<MatOfPoint> { offsetContour }, 
                    -1, new Scalar(0, 165, 255), 3);
                offsetContour.Dispose();
            }
            
            // 添加文字信息
            Imgproc.putText(result, $"Total Pills: {totalPills}", 
                new Point(10, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 1, 
                new Scalar(255, 255, 255), 2);
            Imgproc.putText(result, $"Single: {singleContours.Count}", 
                new Point(10, 70), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, 
                new Scalar(0, 255, 0), 2);
            Imgproc.putText(result, $"Multiple: {multipleContours.Count}", 
                new Point(10, 110), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, 
                new Scalar(0, 0, 255), 2);
            Imgproc.putText(result, $"Reclassified: {reclassifiedContours.Count}", 
                new Point(10, 150), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, 
                new Scalar(0, 165, 255), 2);
            Imgproc.putText(result, $"Ref Area: {referenceArea:F0}", 
                new Point(10, 190), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, 
                new Scalar(255, 255, 255), 2);
            
            return result;
        }
        
        /// <summary>
        /// 偏移轮廓点
        /// </summary>
        private MatOfPoint OffsetContour(MatOfPoint contour, Point offset)
        {
            Point[] points = contour.toArray();
            Point[] offsetPoints = new Point[points.Length];
            
            for (int i = 0; i < points.Length; i++)
            {
                offsetPoints[i] = new Point(
                    points[i].x + offset.x,
                    points[i].y + offset.y
                );
            }
            
            MatOfPoint result = new MatOfPoint();
            result.fromArray(offsetPoints);
            return result;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (background != null)
            {
                background.Dispose();
                background = null;
            }
            
            if (morphKernel != null)
            {
                morphKernel.Dispose();
            }
        }
    }
    
    /// <summary>
    /// 轮廓形状特征
    /// </summary>
    public struct ContourFeatures
    {
        public double Area;
        public double AspectRatio;
        public double Convexity;
        public double Solidity;
        public double Circularity;
        public double Perimeter;
    }
    
    /// <summary>
    /// 计数调试信息
    /// </summary>
    public struct CountingDebugInfo
    {
        public int SinglePillCount;
        public int MultiplePillCount;
        public int ReclassifiedCount;
        public double ReferenceArea;
    }
}
