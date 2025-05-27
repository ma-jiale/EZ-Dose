import RPi.GPIO as GPIO
import time

# 设置 GPIO

GPIO.setmode(GPIO.BOARD)
servo_SIG = 32
servo_freq = 50

GPIO.setup(servo_SIG, GPIO.OUT)

# 初始化 PWM
servo = GPIO.PWM(servo_SIG, servo_freq)
servo.start(0)

def servo_map(angle):
    """将角度转换为 PWM 占空比"""
    return angle / 18 + 2.5

def move_servo():
    """控制舵机转动"""
    try:
        # 设置初始位置为 0 度（平行位置）
        #servo.ChangeDutyCycle(servo_map(0))
        #time.sleep(2)

        # 转到 180 度
        servo.ChangeDutyCycle(servo_map(180))
        time.sleep(2)

        # 顺时针和逆时针转动，重复七次
    except Exception as e:
        print("错误:", str(e))
move_servo()
# 关闭程序时清理 GPIO
servo.stop()
GPIO.cleanup()
