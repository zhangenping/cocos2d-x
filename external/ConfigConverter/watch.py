from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
import time
from build import build_exe
import os
import threading

class CodeChangeHandler(FileSystemEventHandler):
    def __init__(self):
        self.timer = None
        self.debounce_seconds = 1  # 防抖时间设置为1秒
        self.lock = threading.Lock()

    def on_modified(self, event):
        if event.src_path.endswith('main.py'):
            with self.lock:
                # 如果已经有定时器在运行，取消它
                if self.timer:
                    self.timer.cancel()
                
                # 创建新的定时器
                self.timer = threading.Timer(
                    self.debounce_seconds, 
                    self._delayed_build
                )
                self.timer.start()
    
    def _delayed_build(self):
        print("\n检测到 main.py 发生变化，开始重新构建...")
        build_exe()

def watch_code():
    # 获取src目录的绝对路径
    src_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'src')
    
    event_handler = CodeChangeHandler()
    observer = Observer()
    observer.schedule(event_handler, src_path, recursive=False)
    observer.start()
    
    print(f"开始监控 {src_path} 目录下的 main.py 文件变化...")
    print("按 Ctrl+C 停止监控")
    
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
        print("\n停止监控")
    
    observer.join()

if __name__ == "__main__":
    # 首次运行时先构建一次
    print("首次构建...")
    build_exe()
    
    # 然后开始监控文件变化
    watch_code() 