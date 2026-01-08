import os
import subprocess
import shutil
from pathlib import Path

def build_exe():
    print("开始构建可执行文件...")
    
    # 目标目录
    target_dir = r"E:\cocos2d-x\tests\cpp-tests\Classes\Proto"
    
    # 只清理 build 目录
    if os.path.exists('build'):
        shutil.rmtree('build')
    
    try:
        # 运行 PyInstaller
        subprocess.run(['pyinstaller', 'main.spec'], check=True)
        
        # 检查是否成功生成exe
        exe_path = Path('dist/ConfigConverter.exe')
        if exe_path.exists():
            print(f"构建成功！可执行文件位置: {exe_path.absolute()}")
            
            # 复制 ConfigConverter.exe 到目标目录
            target_path = Path(target_dir) / 'ConfigConverter.exe'
            if not target_path.parent.exists():
                print(f"错误：目标目录不存在: {target_dir}")
                return False
                
            # 复制文件
            shutil.copy2(exe_path, target_path)
            print(f"已复制 ConfigConverter.exe 到: {target_dir}")
            return True
        else:
            print("构建失败：未能找到生成的exe文件")
            return False
            
    except subprocess.CalledProcessError as e:
        print(f"构建过程出错: {e}")
        return False
    except Exception as e:
        print(f"发生未知错误: {e}")
        return False

if __name__ == "__main__":
    build_exe() 