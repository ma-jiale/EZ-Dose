import cv2
import numpy as np
import time

def mse(imageA, imageB):
    # 计算均方误差 (MSE)
    err = np.sum((imageA.astype("float") - imageB.astype("float")) ** 2)
    err /= float(imageA.shape[0] * imageA.shape[1])
    return err

# 初始化摄像头
cap = cv2.VideoCapture(1)

# 设置图像尺寸，按需调整
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)

# 初始化背景
background = None

# 设置ROI区域（例如左上角和右下角的坐标），可以根据需求调整
roi_top_left = (20, 20)
roi_bottom_right = (1260, 700)

# 用于记录上一帧图像
prev_frame = None
stable_count = 0
threshold_stable = 3  # 连续3帧保持稳定
last_stable_time = 0

# 主循环
while True:
    # 获取当前时间
    current_time = time.time()

    # 读取摄像头图像
    ret, frame = cap.read()
    if not ret:
        break

    # 提取ROI区域
    roi = frame[roi_top_left[1]:roi_bottom_right[1], roi_top_left[0]:roi_bottom_right[0]]

    # 1. 图像稳定性检查
    gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
    if prev_frame is not None:
        # 计算 MSE
        MES_score = mse(gray, prev_frame)
        print(f"MSE: {MES_score}")

        # 如果差异较小，认为图像稳定
        if MES_score < 10:  # 这个阈值可以根据实际情况调整
            stable_count += 1
        else:
            stable_count = 0

    # 2. 判断图像是否稳定
    if stable_count >= threshold_stable:

        #应用canny边缘检测
        edges = cv2.Canny(gray, 50, 150)
        
        cv2.imshow('Edges', edges)
        
        # 3. 如果ROI中没有边缘，将当前图像加入背景
        if np.count_nonzero(edges) == 0:
            background = gray
            print("No edges detected, updating background...")
            last_stable_time = current_time
            
            # cv2.imshow('background', background)
                
        else:
            # 4. 如果有边缘，执行图像相减操作
            subtracted_img = cv2.subtract(background, gray)

            # 5. 去噪和阈值化
            burred = cv2.GaussianBlur(subtracted_img, (5, 5), 0)

            # 6. 转换为灰度图像并进行二值化
            _, thresh = cv2.threshold(burred, 20, 255, cv2.THRESH_BINARY)

            # 7. 腐蚀和膨胀处理（去噪）
            kernel = np.ones((5, 5), np.uint8)
            erosion = cv2.erode(thresh, kernel, iterations=2)
            dilation = cv2.dilate(erosion, kernel, iterations=2)

            # 8. 查找轮廓
            contours, _ = cv2.findContours(dilation, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
            
            # cv2.imshow('black', thresh)
            # cv2.imshow('subtracted_img', subtracted_img)
            # cv2.imshow('erosion', erosion)
            cv2.imshow('dilation', dilation)

            min_area = 100  # 设置最小面积阈值，过滤掉小的噪声轮廓

            # 9. 轮廓特征提取和计数
            areas = []
            normal_contours = 0  # 用于统计没有凹凸缺陷的轮廓数量
            abnormal_contours = 0  # 用于统计异常药片数
            abnormal_area_threshold = 1.5  # 用于判断异常药片的面积比例
            area_mode = 0  # 计数依据
            sum_area = 0

            # 过滤出没有凹凸缺陷的轮廓
            for cnt in contours:
                area = cv2.contourArea(cnt)
                perimeter = cv2.arcLength(cnt, True)

                # 使用多边形逼近
                epsilon = 0.02 * perimeter  # 逼近精度
                approx = cv2.approxPolyDP(cnt, epsilon, True)  # 简化轮廓为多边形

                # 如果轮廓面积小于最小阈值，跳过该轮廓
                if area < min_area:
                    continue  # 跳过当前轮廓

                convex_hull = cv2.isContourConvex(approx)
                areas.append(area)
    
                if convex_hull:  # 如果轮廓没有凹凸缺陷
                    sum_area += area  # 计算面积总和
                    normal_contours += 1
                    cv2.drawContours(frame, [cnt], -1, (0, 255, 0), 2)  # 绘制绿色轮廓
                    area_mode = sum_area / normal_contours  # 使用平均数作为计数依据

                else:  # 如果轮廓有凹凸缺陷
                    # 判断面积是否小于计数依据的1.5倍
                    if area < area_mode * abnormal_area_threshold:
                        abnormal_contours += 1
                        cv2.drawContours(frame, [cnt], -1, (0, 0, 255), 2)  # 绘制红色轮廓

                    else:
                        # 如果面积大于计数依据的1.5倍，逐步递增除数
                        divisor = 2
                        max_divisor = 10  # 最大除数值，避免无限递增
                        while abs((area / divisor) - area_mode) > area_mode * 0.25 and divisor < max_divisor:
                            divisor += 1
                        # 此时的除数代表一个新的计数依据
                        # area_mode = area / divisor
                        normal_contours += divisor  # 计数药片数
                        cv2.drawContours(frame, [cnt], -1, (0, 255, 255), 2)  # 绘制黄色轮廓

            # 显示计数
            cv2.putText(frame, f'Normal Count: {normal_contours}', (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
            cv2.putText(frame, f'Abnormal Count: {abnormal_contours}', (10, 90), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
    
    # 10. 更新上一帧图像
    prev_frame = gray

    # 显示图像
    cv2.imshow('Original', frame)
    # cv2.imshow('gray', gray)
    # cv2.imshow('prev_frame', prev_frame)

    # 按 'q' 键退出
    if cv2.waitKey(200) & 0xFF == ord('q'):
        break

# 释放摄像头资源并关闭窗口
cap.release()
cv2.destroyAllWindows()