# 创建一个新文件来测试API端点
import requests
import json

def test_api_endpoints():
    """测试API端点"""
    base_url = "https://ixd.sjtu.edu.cn/flask/packer"
    
    # 测试不同的端点组合
    test_configs = [
        {"url": f"{base_url}", "method": "GET"},
        {"url": f"{base_url}/api", "method": "GET"},
        {"url": f"{base_url}/api/patients", "method": "GET"},
        {"url": f"{base_url}/api/prescriptions", "method": "GET"},
        {"url": f"{base_url}/patients", "method": "GET"},
        {"url": f"{base_url}/prescriptions", "method": "GET"},
        {"url": f"{base_url}/status", "method": "GET"},
        {"url": f"{base_url}/health", "method": "GET"},
        # 尝试不带HTTPS
        {"url": f"http://ixd.sjtu.edu.cn/flask/packer", "method": "GET"},
        {"url": f"http://ixd.sjtu.edu.cn/flask/packer/api", "method": "GET"},
    ]
    
    print("=== API端点测试报告 ===")
    for config in test_configs:
        url = config["url"]
        method = config["method"]
        
        try:
            if method == "GET":
                response = requests.get(url, timeout=10)
            
            status_code = response.status_code
            print(f"\n📍 {method} {url}")
            print(f"   状态码: {status_code}")
            
            if status_code == 200:
                print("   ✅ 成功!")
                try:
                    # 尝试解析JSON
                    json_data = response.json()
                    print(f"   📄 JSON响应: {json.dumps(json_data, indent=2, ensure_ascii=False)[:300]}...")
                except:
                    # 如果不是JSON，显示文本内容
                    text_content = response.text[:300]
                    print(f"   📄 文本响应: {text_content}...")
            elif status_code == 404:
                print("   ❌ 端点不存在")
            elif status_code == 405:
                print("   ⚠️  方法不允许")
            else:
                print(f"   ⚠️  其他状态: {status_code}")
                print(f"   📄 响应: {response.text[:200]}...")
                
        except requests.exceptions.RequestException as e:
            print(f"   💥 连接错误: {str(e)}")
    
    print("\n=== 测试完成 ===")

if __name__ == "__main__":
    test_api_endpoints()