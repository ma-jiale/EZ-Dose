import cv2
import numpy as np
from collections import deque
import statistics

class PillCounter:
    def __init__(self, camera_id=1):
        """
        初始化药片计数器
        Args:
            camera_id: 摄像头ID，默认为1，如果为None则不初始化摄像头
        """
        if camera_id is not None:
            self.cap = cv2.VideoCapture(camera_id)
        else:
            self.cap = None
            
        self.background = None
        self.background_captured = False

        self.edge_threshold = 1000  # 边缘检测阈值
        self.recent_edge_counts = deque(maxlen=10)  # 存储最近的边缘数量
        self.stable_frames_needed = 15  # 需要稳定的帧数
        self.stable_count = 0
        
        # 画面裁切参数（去除边缘杂物）
        self.crop_margin = 50  # 裁切边距
        
        # 形态学操作参数
        self.morph_kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
        
        # 轮廓过滤参数
        self.min_contour_area = 50
        self.max_contour_area = 100000  # 防止检测到过大的区域
        self.convexity_threshold = 0.90  # 提高凸包度阈值，更严格
        
        # 形状分析参数
        self.aspect_ratio_threshold = 3.0  # 长宽比阈值，胶囊通常细长
        self.solidity_threshold = 0.85  # 实心度阈值
        
    def crop_frame(self, frame):
        """
        裁切画面，去除边缘杂物
        Args:
            frame: 输入图像
        Returns:
            cropped_frame: 裁切后的图像
        """
        h, w = frame.shape[:2]
        return frame[self.crop_margin:h-self.crop_margin, 
                    self.crop_margin:w-self.crop_margin]
        
    def detect_edges(self, frame):
        """
        检测图像中的边缘
        Args:
            frame: 输入图像
        Returns:
            edge_count: 边缘像素数量
        """
        cropped = self.crop_frame(frame)
        gray = cv2.cvtColor(cropped, cv2.COLOR_BGR2GRAY)
        blurred = cv2.GaussianBlur(gray, (5, 5), 0)
        edges = cv2.Canny(blurred, 50, 150)
        edge_count = np.sum(edges > 0)
        return edge_count, edges
    
    def is_scene_stable(self, edge_count):
        """
        判断场景是否稳定（适合作为背景）
        Args:
            edge_count: 当前帧的边缘数量
        Returns:
            bool: 是否稳定
        """
        self.recent_edge_counts.append(edge_count)
        
        if len(self.recent_edge_counts) < self.recent_edge_counts.maxlen:
            return False
        
        # 计算边缘数量的方差，如果方差小说明场景稳定
        variance = np.var(list(self.recent_edge_counts))
        mean_edges = np.mean(list(self.recent_edge_counts))
        
        # 如果方差小且边缘数量不太多，认为场景稳定
        if variance < 8000 and mean_edges < self.edge_threshold:
            self.stable_count += 1
            return self.stable_count >= self.stable_frames_needed
        else:
            self.stable_count = 0
            return False
    
    def capture_background(self, frame):
        """
        捕捉背景图像
        Args:
            frame: 当前帧
        """
        cropped = self.crop_frame(frame)
        gray = cv2.cvtColor(cropped, cv2.COLOR_BGR2GRAY)
        self.background = cv2.GaussianBlur(gray, (5, 5), 0)
        self.background_captured = True
        print("背景已捕捉")
    
    def preprocess_image(self, frame):
        """
        图像预处理：背景减法、二值化和腐蚀操作
        Args:
            frame: 当前帧
        Returns:
            binary: 预处理后的二值化图像
        """
        cropped = self.crop_frame(frame)
        gray = cv2.cvtColor(cropped, cv2.COLOR_BGR2GRAY)
        blurred = cv2.GaussianBlur(gray, (5, 5), 0)
        
        # 背景减法
        diff = cv2.absdiff(self.background, blurred)
        
        # 二值化，使用更严格的阈值
        _, binary = cv2.threshold(diff, 40, 255, cv2.THRESH_BINARY)
        
        # 形态学操作去除噪声，保持分离效果
        binary = cv2.morphologyEx(binary, cv2.MORPH_OPEN, self.morph_kernel)
        
        # 腐蚀操作：断开轻微相连的轮廓
        erosion_kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (6, 6))
        binary = cv2.erode(binary, erosion_kernel, iterations=2)
        
        # # 膨胀操作：恢复轮廓大小，但保持分离效果
        # dilation_kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
        # binary = cv2.dilate(binary, dilation_kernel, iterations=2)
        
        return binary
    
    def analyze_contour_shape(self, contour):
        """
        分析轮廓的形状特征
        Args:
            contour: 输入轮廓
        Returns:
            dict: 形状特征字典
        """
        area = cv2.contourArea(contour)
        
        # 计算轮廓的边界矩形
        x, y, w, h = cv2.boundingRect(contour)
        aspect_ratio = max(w, h) / min(w, h) if min(w, h) > 0 else 0
        
        # 计算凸包
        hull = cv2.convexHull(contour)
        hull_area = cv2.contourArea(hull)
        convexity = area / hull_area if hull_area > 0 else 0
        
        # 计算实心度 (solidity)
        solidity = area / hull_area if hull_area > 0 else 0
        
        # 计算周长
        perimeter = cv2.arcLength(contour, True)
        
        # 计算圆形度 (4*pi*area/perimeter^2)
        circularity = 4 * np.pi * area / (perimeter * perimeter) if perimeter > 0 else 0
        
        return {
            'area': area,
            'aspect_ratio': aspect_ratio,
            'convexity': convexity,
            'solidity': solidity,
            'circularity': circularity,
            'perimeter': perimeter
        }
    
    def is_single_pill(self, contour):
        """
        判断轮廓是否为单个药片（更严格的判断）
        Args:
            contour: 输入轮廓
        Returns:
            bool: 是否为单个药片
        """
        features = self.analyze_contour_shape(contour)
        
        # 面积过滤
        if features['area'] < self.min_contour_area or features['area'] > self.max_contour_area:
            return False
        
        # 多重判断条件
        is_convex = features['convexity'] >= self.convexity_threshold
        is_solid = features['solidity'] >= self.solidity_threshold
        is_reasonable_ratio = features['aspect_ratio'] <= self.aspect_ratio_threshold
        is_circular_enough = features['circularity'] > 0.3  # 不能太细长
        
        # 综合判断：必须同时满足多个条件
        return is_convex and is_solid and is_reasonable_ratio and is_circular_enough
    
    def detect_multiple_pills_by_area(self, contour, reference_area):
        """
        基于面积检测多个药片
        Args:
            contour: 输入轮廓
            reference_area: 参考药片面积
        Returns:
            estimated_count: 估算的药片数量
        """
        if reference_area == 0:
            return 1
        
        contour_area = cv2.contourArea(contour)
        ratio = contour_area / reference_area
        
        # 更精确的估算
        if ratio < 0.7:
            return 0  # 太小，可能是噪声
        elif ratio <= 1.2:
            return 1  # 单个药片
        elif ratio <= 2.4:
            return 2  # 两个药片
        elif ratio <= 3.6:
            return 3  # 三个药片
        elif ratio <= 4.8:
            return 4  # 三个药片
        else:
            return max(1, round(ratio))  # 更多药片
    
    def detect_multiple_pills_by_geometry(self, contour):
        """
        基于几何特征检测多个药片
        Args:
            contour: 输入轮廓
        Returns:
            estimated_count: 估算的药片数量
        """
        features = self.analyze_contour_shape(contour)
        
        # 如果长宽比很大，可能是多个胶囊排列
        if features['aspect_ratio'] > 2.5:
            # 根据长宽比估算数量
            estimated = max(1, round(features['aspect_ratio'] / 2.5))
            return min(estimated, 6)  # 最多估算4个
        
        return 1
    
    def calculate_reference_area(self, single_pill_contours):
        """
        计算参考药片面积（使用中位数更稳定）
        Args:
            single_pill_contours: 单个药片轮廓列表
        Returns:
            reference_area: 参考面积
        """
        if not single_pill_contours:
            return 0
        
        areas = [cv2.contourArea(contour) for contour in single_pill_contours]
        
        # 使用中位数
        median_area = statistics.median(areas)
        
        # 过滤异常值（距离中位数太远的值）
        filtered_areas = [area for area in areas 
                         if 0.6 * median_area <= area <= 1.4 * median_area]
        
        if filtered_areas:
            return statistics.median(filtered_areas)
        else:
            return median_area
    
    def count_pills(self, frame):
        """
        主要的药片计数函数（优化轮廓分离）
        Args:
            frame: 输入图像
        Returns:
            pill_count: 药片数量
            result_frame: 结果图像（带标注）
        """
        if not self.background_captured:
            return 0, frame
        
        # # 裁切画面
        cropped_frame = self.crop_frame(frame)
        
        # 预处理图像（包含腐蚀分离）
        binary = self.preprocess_image(frame)
        # cv2.imshow("第一次腐蚀操作", binary)
        # 额外的轮廓分离处理
        processed_binary = self.separate_contours(binary)
        # cv2.imshow("第二次腐蚀操作", binary)
        
        # 查找轮廓
        contours, _ = cv2.findContours(processed_binary, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        
        # 过滤轮廓
        valid_contours = []
        for contour in contours:
            area = cv2.contourArea(contour)
            if self.min_contour_area <= area <= self.max_contour_area:
                valid_contours.append(contour)
        
        if not valid_contours:
            return 0, frame
        
        # 分类轮廓
        single_pill_contours = []
        multiple_pill_contours = []
        
        for contour in valid_contours:
            if self.is_single_pill(contour):
                single_pill_contours.append(contour)
            else:
                multiple_pill_contours.append(contour)
        
        # 计算参考面积
        reference_area = self.calculate_reference_area(single_pill_contours)

        # 将大于基准面积120%的single_pill_contours 打入 multiple_pill_contours
        if reference_area > 0:
            area_threshold = reference_area * 1.2  # 改回120%阈值，更精确
            final_single_contours = []
            reclassified_contours = []
            
            for contour in single_pill_contours:
                contour_area = cv2.contourArea(contour)
                if contour_area > area_threshold:
                    # 面积过大，重新分类为多药片
                    multiple_pill_contours.append(contour)
                    reclassified_contours.append(contour)
                    print(f"轮廓重新分类: 面积{contour_area:.0f} > 阈值{area_threshold:.0f}")
                else:
                    # 保持为单个药片
                    final_single_contours.append(contour)
            
            # 更新单个药片轮廓列表
            single_pill_contours = final_single_contours
            
            # 重新计算参考面积（基于重新分类后的单个药片）
            if single_pill_contours:
                reference_area = self.calculate_reference_area(single_pill_contours)
        
            print(f"重新分类完成: 单个药片{len(single_pill_contours)}个, 多药片{len(multiple_pill_contours)}个")
        else:
            reclassified_contours = []
        
        # 计算总药片数量
        total_pills = len(single_pill_contours)  # 单个药片直接计数
        
        # 处理多药片轮廓
        for contour in multiple_pill_contours:
            # 结合面积和几何特征估算
            estimated_pills = area_estimate = self.detect_multiple_pills_by_area(contour, reference_area)
            # geometry_estimate = self.detect_multiple_pills_by_geometry(contour)
            
            # # 取较大值作为最终估算（更保守）
            # estimated_pills = max(area_estimate, geometry_estimate)
            total_pills += estimated_pills
        
        # 绘制结果（在原始frame上，考虑裁切偏移）
        result_frame = frame.copy()
        
        # 绘制裁切区域边界
        h, w = frame.shape[:2]
        cv2.rectangle(result_frame, 
                     (self.crop_margin, self.crop_margin), 
                     (w-self.crop_margin, h-self.crop_margin), 
                     (255, 255, 0), 2)
        
        # 调整轮廓坐标（考虑裁切偏移）
        offset_single = []
        offset_multiple = []
        offset_reclassified = []
        
        for contour in single_pill_contours:
            offset_contour = contour + [self.crop_margin, self.crop_margin]
            offset_single.append(offset_contour)
        
        for contour in multiple_pill_contours:
            offset_contour = contour + [self.crop_margin, self.crop_margin]
            offset_multiple.append(offset_contour)
        
        # 单独处理重新分类的轮廓，用于特殊显示
        for contour in reclassified_contours:
            offset_contour = contour + [self.crop_margin, self.crop_margin]
            offset_reclassified.append(offset_contour)
        
        # 绘制轮廓
        cv2.drawContours(result_frame, offset_single, -1, (0, 255, 0), 2)  # 绿色：单个药片
        cv2.drawContours(result_frame, offset_multiple, -1, (0, 0, 255), 2)  # 红色：多个药片
        
        # 为重新分类的轮廓添加橙色边框（表示这些是被重新分类的）
        if offset_reclassified:
            cv2.drawContours(result_frame, offset_reclassified, -1, (0, 165, 255), 3)  # 橙色：重新分类
        
        # 添加详细信息
        cv2.putText(result_frame, f"Total Pills: {total_pills}", (10, 30), 
                   cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2)
        cv2.putText(result_frame, f"Single: {len(single_pill_contours)}", (10, 70), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
        cv2.putText(result_frame, f"Multiple: {len(multiple_pill_contours)}", (10, 110), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)
        cv2.putText(result_frame, f"Reclassified: {len(reclassified_contours)}", (10, 150), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 165, 255), 2)
        cv2.putText(result_frame, f"Ref Area: {reference_area:.0f}", (10, 190), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
        
        return total_pills, result_frame
    
    def separate_contours(self, binary):
        """
        额外的轮廓分离处理
        Args:
            binary: 输入二值化图像
        Returns:
            separated_binary: 分离后的二值化图像
        """
        # 使用更强的腐蚀来分离粘连的轮廓
        separation_kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
        eroded = cv2.erode(binary, separation_kernel, iterations=2)
        
        # 查找连通组件
        num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats(eroded, connectivity=8)
        
        # 创建分离后的图像
        separated = np.zeros_like(binary)
        
        # 对每个连通组件进行膨胀恢复
        recovery_kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (4, 4))
        
        for i in range(1, num_labels):  # 跳过背景标签0
            # 提取单个连通组件
            component_mask = (labels == i).astype(np.uint8) * 255
            
            # 膨胀恢复大小
            recovered = cv2.dilate(component_mask, recovery_kernel, iterations=2)
            
            # 合并到结果图像
            separated = cv2.bitwise_or(separated, recovered)
        
        return separated
    
    def run(self):
        """
        运行药片计数程序
        """
        if self.cap is None:
            print("错误: 未初始化摄像头")
            return
            
        print("药片计数器启动")
        print("按 'b' 重新捕捉背景")
        print("按 'q' 退出程序")
        
        while True:
            ret, frame = self.cap.read()
            if not ret:
                print("无法读取摄像头")
                break
            
            # 检测边缘
            edge_count, edges = self.detect_edges(frame)
            
            # 如果还没有背景，尝试捕捉
            if not self.background_captured:
                if self.is_scene_stable(edge_count):
                    self.capture_background(frame)
                else:
                    # 显示等待背景的状态
                    display_frame = frame.copy()
                    # 显示裁切区域
                    h, w = frame.shape[:2]
                    cv2.rectangle(display_frame, 
                                 (self.crop_margin, self.crop_margin), 
                                 (w-self.crop_margin, h-self.crop_margin), 
                                 (0, 255, 255), 2)
                    cv2.putText(display_frame, "Waiting for stable background...", 
                               (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
                    cv2.putText(display_frame, f"Edge count: {edge_count}", 
                               (10, 70), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)
                    cv2.imshow('Pill Counter', display_frame)
            else:
                # 进行药片计数
                pill_count, result_frame = self.count_pills(frame)
                cv2.imshow('Pill Counter', result_frame)
                
                # 显示二值化图像（调试用）
                binary = self.preprocess_image(frame)
                cv2.imshow('Binary', binary)
            
            # 处理按键
            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                break
            elif key == ord('b'):
                self.background_captured = False
                self.stable_count = 0
                self.recent_edge_counts.clear()
                print("重新捕捉背景...")
        
        if self.cap:
            self.cap.release()
        cv2.destroyAllWindows()

def main():
    """
    主函数
    """
    try:
        counter = PillCounter(camera_id=1)
        counter.run()
    except Exception as e:
        print(f"错误: {e}")

if __name__ == "__main__":
    main()