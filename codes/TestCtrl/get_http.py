import requests
import pandas as pd
import os
import time

class Client():
    def __init__(self, username) -> None:
        self.username = username
        pass

    #拉取管理员的所有病人的处方数据
    def get_data(self, api_endpoint):
        params = {'username': self.username}
        response = requests.get(api_endpoint, params=params)

        if response.status_code == 200:
            data = response.json()
            flattened_data = []
            for user_data in data:
                if 'patients' in user_data:
                    patients = user_data.pop('patients') 
                    for patient_data in patients:
                        if 'medications' in patient_data:
                            medications = patient_data.pop('medications')
                            for medication_data in medications:
                                user_patient_medication_data = {**user_data, **patient_data, **medication_data}
                                flattened_data.append(user_patient_medication_data)
                else:
                    flattened_data.append(user_data)
            
            df = pd.DataFrame(flattened_data) #处理好的病人和对应的药物信息
            return df
        else:
            print(f"Failed to retrieve data. Status code: {response.status_code}")

    def upload_data(self, image_file_path, upload_url):
        if not os.path.exists(image_file_path):
            print("图像文件不存在。")
            return

        # 打开图像文件
        with open(image_file_path, 'rb') as image_file:
            # 构建文件对象
            files = {'image': image_file}
            data = {'username': self.username}  # 添加 username 数据

            # 发送HTTP POST请求
            response = requests.post(upload_url, files=files, data=data)
        # 检查响应状态码
        if response.status_code == 200:
            print("图像上传成功！")
        else:
            print(f"图像上传失败。状态码: {response.status_code}")


if __name__ == '__main__':
    upload_url = 'http://202.120.61.207/upload'
    api_endpoint = 'http://202.120.61.207/UserList'
    username = 'wkk'#根据不同的用户进行修改
    image_file_path = '/home/wkk/wkk/django/MedicineServer/training_process_plot.png' #修改图片路径
    client = Client(username)
    df = client.get_data(api_endpoint)
    print(df.loc[0]['morning_dosage'])
    #client.upload_data(image_file_path, upload_url)

