import xml.etree.ElementTree as ET
import os
import shutil
import subprocess
from pathlib import Path
import time
import hashlib

class Logger:
    def __init__(self):
        self.log_dir = Path("log")
        self.log_dir.mkdir(exist_ok=True)
        self.general_log = self.log_dir / "py_generate.log"
        self.convert_log = None
        self.protoc_log = None
        self.error_log = None

    def init_session(self):
        """初始化新的会话日志文件"""
        timestamp = time.strftime("%Y%m%d_%H%M%S")
        self.convert_log = self.log_dir / f"convert_{timestamp}.log"
        self.protoc_log = self.log_dir / f"protoc_{timestamp}.log"
        self.error_log = self.log_dir / f"error_{timestamp}.log"

    def log(self, message, show_console=False, category="general"):
        """
        记录日志
        category: "general", "convert", "protoc", "error"
        """
        try:
            timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
            log_message = f"[{timestamp}] {message}\n"
            
            # 选择日志文件
            if category == "convert" and self.convert_log:
                log_file = self.convert_log
            elif category == "protoc" and self.protoc_log:
                log_file = self.protoc_log
            elif category == "error" and self.error_log:
                log_file = self.error_log
            else:
                log_file = self.general_log

            # 写入日志
            with open(log_file, 'a', encoding='utf-8') as f:
                f.write(log_message)
            
            # 如果需要，同时显示到控制台
            if show_console:
                print(message)
                
        except Exception as e:
            print(f"写入日志出错: {str(e)}")

    def get_convert_log(self):
        return self.convert_log

    def get_protoc_log(self):
        return self.protoc_log

    def get_error_log(self):
        return self.error_log

# 创建全局日志实例
logger = Logger()

def check_excel2config():
    """检查Excel2Config.exe是否存在"""
    exe_path = Path("Excel2Config.exe")
    if not exe_path.exists():
        logger.log("错误：Excel2Config.exe 未找到，请确保其位于当前目录。", show_console=True)
        return False
    return True

def get_working_directory():
    try:
        user_file_path = r"..\..\proj.win32\cpp-tests.vcxproj.user"
        
        if not os.path.exists(user_file_path):
            logger.log(f"错误：找不到文件 {user_file_path}", show_console=True)
            return None
            
        tree = ET.parse(user_file_path)
        root = tree.getroot()
        
        # 获取XML命名空间
        namespaces = {'ns': 'http://schemas.microsoft.com/developer/msbuild/2003'}

        user_file_abs = os.path.abspath(user_file_path)
        project_dir = os.path.dirname(user_file_abs)
        project_dir = os.path.join(project_dir, "")  # 这行是关键！
        
        # 使用命名空间查找PropertyGroup节点
        for prop_group in root.findall(".//ns:PropertyGroup", namespaces):
            condition = prop_group.get('Condition', '')
            logger.log(f"检查PropertyGroup: {condition}", show_console=True)
            platform_target = "Win32" # Win32/x64
            
            if platform_target in condition:
                # 使用命名空间查找LocalDebuggerWorkingDirectory节点
                working_dir_elem = prop_group.find("ns:LocalDebuggerWorkingDirectory", namespaces)
                if working_dir_elem is not None and working_dir_elem.text:
                    working_dir = working_dir_elem.text.strip()
                    # 1. 替换$(ProjectDir)为实际目录
                    working_dir = working_dir.replace("$(ProjectDir)", project_dir)
                    # 2. 标准化路径（自动处理..、/、\等，生成绝对路径）
                    working_dir_abs = os.path.abspath(working_dir)
                    logger.log(f"找到{platform_target}配置的工作目录：{working_dir_abs}", show_console=True)
                    return working_dir_abs
                else:
                    logger.log("PropertyGroup中未找到有效的LocalDebuggerWorkingDirectory", show_console=True)
        
        # 如果上面的方法失败，尝试不使用命名空间
        logger.log("尝试不使用命名空间重新查找...", show_console=True)
        for prop_group in root.findall(".//PropertyGroup"):
            condition = prop_group.get('Condition', '')
            logger.log(f"检查PropertyGroup: {condition}", show_console=True)
            
            if platform_target in condition:
                working_dir_elem = prop_group.find("LocalDebuggerWorkingDirectory")
                if working_dir_elem is not None and working_dir_elem.text:
                    working_dir = working_dir_elem.text.strip()
                    # 1. 替换$(ProjectDir)为实际目录
                    working_dir = working_dir.replace("$(ProjectDir)", project_dir)
                    # 2. 标准化路径（自动处理..、/、\等，生成绝对路径）
                    working_dir_abs = os.path.abspath(working_dir)
                    logger.log(f"找到{platform_target}配置的工作目录：{working_dir_abs}", show_console=True)
                    return working_dir_abs
                
        logger.log("未找到{platform_target}配置的LocalDebuggerWorkingDirectory", show_console=True)
        return None
        
    except ET.ParseError as e:
        logger.log(f"XML解析错误：{str(e)}", show_console=True)
        return None
    except Exception as e:
        logger.log(f"读取配置文件时出错：{str(e)}", show_console=True)
        return None

def convert_txt_file(input_path, output_dir, working_dir):
    """
    使用Excel2Config.exe转换目录或文件
    input_path: 输入目录或文件的路径
    output_dir: 输出目录的路径
    working_dir: 工作目录
    """
    try:
        if not check_excel2config():
            logger.log("无法进行转换：缺少必要的Excel2Config.exe程序", show_console=True, category="error")
            return False
        
        # 初始化新的会话日志
        logger.init_session()

        # 清空output目录
        output_path = os.path.join(working_dir, 'OutPut')
        if os.path.exists(output_path):
            shutil.rmtree(output_path)
            logger.log(f"已清空目录: {output_path}", show_console=True)
        
        # 构建命令
        rel_input = os.path.relpath(input_path, working_dir)
        cmd = [
            "Excel2Config.exe",
            f"--excel_path={rel_input}",
            "--output_path=OutPut",
            "--to_protobuf=all",
            "--protoc=protoc.exe",
            "--protoc_cmd="
        ]
        
        logger.log(f"执行命令: {' '.join(cmd)}", show_console=True, category="convert")
        
        # 执行命令
        result = subprocess.run(
            cmd,
            cwd=working_dir,
            stdout=open(logger.get_convert_log(), 'w', encoding='utf-8'),
            stderr=open(logger.get_error_log(), 'w', encoding='utf-8'),
            text=True
        )
        
        if result.returncode == 0:
            logger.log(f"成功转换文件: {os.path.basename(input_path)}", show_console=True, category="convert")
            # 如果错误日志为空，删除它
            if logger.get_error_log().stat().st_size == 0:
                logger.get_error_log().unlink()
            
            # 1. 复制 binary 文件到目标目录
            output_binary = os.path.join(working_dir, 'OutPut', 'binary')
            if os.path.exists(output_binary):
                # 遍历 binary 目录下的所有文件
                for file_name in os.listdir(output_binary):
                    src_file = os.path.join(output_binary, file_name)
                    dst_file = os.path.join(output_dir, file_name)
                    if os.path.isfile(src_file):
                        shutil.copy2(src_file, dst_file)
                logger.log(f"已复制生成的binary文件到: {output_dir}", show_console=True)
            
            # 2. 复制 proto 文件到当前目录下的 src/GameBase/ProtoConfig
            output_proto = os.path.join(working_dir, 'OutPut', 'proto')
            # 使用当前工作目录
            proto_target = os.path.join(os.getcwd(), 'src', 'GameBase', 'ProtoConfig')
            
            if os.path.exists(output_proto):
                # 确保目标目录存在
                os.makedirs(proto_target, exist_ok=True)
                
                # 遍历 proto 目录下的所有文件
                for file_name in os.listdir(output_proto):
                    src_file = os.path.join(output_proto, file_name)
                    dst_file = os.path.join(proto_target, file_name)
                    if os.path.isfile(src_file):
                        shutil.copy2(src_file, dst_file)
                logger.log(f"已复制生成的proto文件到: {proto_target}", show_console=True)
            
            return True
        else:
            logger.log(f"转换失败: {os.path.basename(input_path)}", show_console=True, category="error")
            return False
            
    except Exception as e:
        logger.log(f"转换文件出错: {str(e)}", show_console=True, category="error")
        return False

def process_files(working_dir):
    # 构建源目录和目标目录的完整路径
    source_dir = os.path.join(working_dir, 'ini', 'client', 'common', 'datatable')
    target_dir = os.path.join(working_dir, 'ini', 'client', 'common', 'databytes')
    
    # 检查源目录是否存在
    if not os.path.exists(source_dir):
        logger.log(f"错误：源目录不存在 {source_dir}", show_console=True)
        return False
    
    # 创建目标目录（如果不存在）
    os.makedirs(target_dir, exist_ok=True)
    
    # 直接转换整个目录
    return convert_txt_file(source_dir, target_dir, working_dir)

def modify_cc_file(cc_file_path, proto_name):
    """
    修改生成的cc文件中的特定字符串
    cc_file_path: cc文件的路径
    proto_name: proto文件的名称（不含扩展名）
    """
    try:
        # 读取文件内容
        with open(cc_file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 执行替换
        replacements = {
            'PROTOBUF_PRAGMA_INIT_SEG': '//PROTOBUF_PRAGMA_INIT_SEG',
            'schemas': f'schemas_{proto_name}',
            'file_default_instances': f'file_default_instances_{proto_name}'
        }
        
        for old, new in replacements.items():
            content = content.replace(old, new)
        
        # 写回文件
        with open(cc_file_path, 'w', encoding='utf-8') as f:
            f.write(content)
            
        logger.log(f"已修改文件: {os.path.basename(cc_file_path)}", show_console=True)
        return True
        
    except Exception as e:
        logger.log(f"修改文件 {cc_file_path} 时出错: {str(e)}", show_console=True)
        return False

def get_file_hash(file_path):
    """
    计算文件的MD5哈希值
    """
    try:
        with open(file_path, 'rb') as f:
            md5_hash = hashlib.md5()
            # 分块读取文件以处理大文件
            for chunk in iter(lambda: f.read(4096), b''):
                md5_hash.update(chunk)
        return md5_hash.hexdigest()
    except Exception as e:
        logger.log(f"计算文件哈希值时出错 {file_path}: {str(e)}", show_console=True)
        return None

def copy_generated_files():
    """
    复制生成的文件到上层目录的对应位置，并将.cc文件重命名为.cpp
    只在文件内容发生变化时才复制
    """
    try:
        dst_dir = Path('dst')
        if not dst_dir.exists():
            logger.log("错误：dst目录不存在", show_console=True)
            return False
            
        # 获取上层目录的绝对路径
        parent_dir = os.path.abspath('..')
        files_copied = 0
        files_skipped = 0
        
        # 遍历dst目录
        for root, _, files in os.walk(dst_dir):
            # 获取相对于dst的路径
            rel_path = os.path.relpath(root, dst_dir)
            # 构建目标目录
            target_dir = os.path.join(parent_dir, rel_path)
            
            # 确保目标目录存在
            os.makedirs(target_dir, exist_ok=True)
            
            for file in files:
                src_file = os.path.join(root, file)
                
                # 对于.cc文件，改名为.cpp
                if file.endswith('.cc'):
                    target_file = os.path.join(target_dir, file[:-3] + '.cpp')
                else:
                    target_file = os.path.join(target_dir, file)
                
                # 计算源文件的哈希值
                src_hash = get_file_hash(src_file)
                
                # 检查目标文件是否存在并计算其哈希值
                if os.path.exists(target_file):
                    target_hash = get_file_hash(target_file)
                    if src_hash == target_hash:
                        logger.log(f"跳过相同文件: {os.path.relpath(target_file, parent_dir)}", show_console=True)
                        files_skipped += 1
                        continue
                
                # 复制文件
                shutil.copy2(src_file, target_file)
                files_copied += 1
                logger.log(f"已复制: {os.path.relpath(src_file, dst_dir)} -> {os.path.relpath(target_file, parent_dir)}", show_console=True)
        
        logger.log(f"\n复制完成: {files_copied} 个文件已更新, {files_skipped} 个文件未变化", show_console=True)
        return True
        
    except Exception as e:
        logger.log(f"复制生成的文件时出错: {str(e)}", show_console=True)
        return False

def normalize_path(path):
    """
    标准化路径，统一使用正斜杠，并转换为小写以进行比较
    """
    return path.replace('\\', '/').lower()

def update_vcxproj():
    """
    更新GameBase.vcxproj文件，添加新生成的.h和.cpp文件
    """
    try:
        vcxproj_path = os.path.abspath('../GameBase/GameBase.vcxproj')
        logger.log(f"检查项目文件路径: {vcxproj_path}", show_console=True)
        
        if not os.path.exists(vcxproj_path):
            logger.log("错误：找不到项目文件", show_console=True)
            return False

        with open(vcxproj_path, 'r', encoding='utf-8') as f:
            content = f.readlines()

        existing_includes = set()
        existing_compiles = set()
        
        for line in content:
            if '<ClInclude Include="' in line:
                path = line.split('Include="')[1].split('"')[0]
                existing_includes.add(normalize_path(path))
            elif '<ClCompile Include="' in line:
                path = line.split('Include="')[1].split('"')[0]
                existing_compiles.add(normalize_path(path))

        logger.log("现有文件列表:")
        for path in existing_compiles:
            logger.log(f"  现有cpp: {path}", show_console=True)
        for path in existing_includes:
            logger.log(f"  现有h: {path}", show_console=True)

        include_group_end = -1
        compile_group_end = -1
        
        for i, line in enumerate(content):
            if '<ClInclude Include="' in line:
                for j in range(i, len(content)):
                    if '</ItemGroup>' in content[j]:
                        include_group_end = j
                        break
            elif '<ClCompile Include="' in line:
                for j in range(i, len(content)):
                    if '</ItemGroup>' in content[j]:
                        compile_group_end = j
                        break

        if include_group_end == -1 or compile_group_end == -1:
            logger.log("错误：未找到必要的ItemGroup", show_console=True)
            return False

        new_includes = []
        new_compiles = []
        
        for root_dir, _, files in os.walk('../GameBase'):
            for file in files:
                if not (file.endswith('.pb.h') or file.endswith('.pb.cpp')):
                    continue
                    
                rel_path = os.path.relpath(os.path.join(root_dir, file), '../GameBase')
                rel_path = rel_path.replace('\\', '/')
                normalized_path = normalize_path(rel_path)
                
                if file.endswith('.pb.h'):
                    if normalized_path not in existing_includes:
                        new_includes.append(f'    <ClInclude Include="{rel_path}" />\n')
                        logger.log(f"添加头文件: {rel_path}", show_console=True)
                    else:
                        logger.log(f"跳过已存在的头文件: {rel_path}", show_console=True)
                        
                elif file.endswith('.pb.cpp'):
                    if normalized_path not in existing_compiles:
                        new_compiles.append(f'    <ClCompile Include="{rel_path}" />\n')
                        logger.log(f"添加源文件: {rel_path}", show_console=True)
                    else:
                        logger.log(f"跳过已存在的源文件: {rel_path}", show_console=True)

        if new_includes or new_compiles:
            if new_includes:
                content.insert(include_group_end, ''.join(new_includes))
            if new_compiles:
                content.insert(compile_group_end, ''.join(new_compiles))
            
            with open(vcxproj_path, 'w', encoding='utf-8') as f:
                f.writelines(content)
            
            logger.log(f"已添加 {len(new_includes) + len(new_compiles)} 个新文件到项目", show_console=True)
        else:
            logger.log("没有新文件需要添加到项目", show_console=True)
            
        return True
        
    except Exception as e:
        logger.log(f"更新项目文件时出错: {str(e)}", show_console=True)
        return False

def compile_proto_files():
    """
    前工作目录下src目中的所有proto文件，保持目录结构
    """
    try:
        # 检查protoc.exe是否存在
        if not Path("protoc.exe").exists():
            logger.log("错误：protoc.exe 未找到，请确保其位于当前目录。", show_console=True)
            return False
            
        # 创建日志目录和输出目录
        log_dir = Path("log")
        dst_dir = Path("dst")
        log_dir.mkdir(exist_ok=True)
        dst_dir.mkdir(exist_ok=True)
        
        # 设置日志文件
        timestamp = time.strftime("%Y%m%d_%H%M%S")
        log_file = log_dir / f"protoc_{timestamp}.log"
        error_log = log_dir / f"protoc_error_{timestamp}.log"
        
        # 获取当前目录下的src目录
        src_dir = os.path.join(os.getcwd(), 'src')
        if not os.path.exists(src_dir):
            logger.log(f"错误：src目录不存在: {src_dir}", show_console=True)
            return False
            
        # 获取当前目录的绝对路径
        current_dir = os.path.abspath('.')
        
        # 只收集src目录下的proto文件
        proto_files = []
        for root, _, files in os.walk(src_dir):
            for file in files:
                if file.endswith('.proto'):
                    # 使用绝对路径
                    proto_files.append(os.path.abspath(os.path.join(root, file)))
        
        if not proto_files:
            logger.log("未找到任何.proto文件", show_console=True)
            return False
            
        logger.log(f"找到 {len(proto_files)} 个proto文件", show_console=True)
        
        # 为每个proto文件运行protoc
        with open(log_file, 'w', encoding='utf-8') as log, \
             open(error_log, 'w', encoding='utf-8') as err_log:
            
            log.write(f"=== Proto编译开始时间: {timestamp} ===\n")
            
            for proto_path in proto_files:
                # 获取相对于src的路径
                if 'GameBase' in proto_path:
                    dll_option = "dllexport_decl=PROTOBUF_API:"
                else:
                    dll_option = ""

                # 获取相对于src的路径
                rel_path = os.path.relpath(os.path.dirname(proto_path), src_dir)
                # 创建对应的dst目录
                dst_path = os.path.join(current_dir, 'dst', rel_path)
                os.makedirs(dst_path, exist_ok=True)
                
                proto_name = os.path.splitext(os.path.basename(proto_path))[0]
                logger.log(f"正在编译: {proto_name}", show_console=True)
                
                # 构建protoc命令，使用绝对路径
                cmd = [
                    "protoc.exe",
                    f"--proto_path={os.path.dirname(proto_path)}",
                    f"--cpp_out={dll_option}{dst_path}",
                    proto_path
                ]
                
                log.write(f"\n正在编译: {proto_path}\n")
                log.write(f"输出目录: {dst_path}\n")
                log.write(f"命令: {' '.join(cmd)}\n")
                
                # 执行protoc命令
                result = subprocess.run(
                    cmd,
                    stdout=log,
                    stderr=err_log,
                    text=True
                )
                
                if result.returncode != 0:
                    logger.log(f"编译失败: {proto_name}", show_console=True)
                    return False
                
                # 修改生成的cc文件
                cc_file = os.path.join(dst_path, f"{proto_name}.pb.cc")
                if os.path.exists(cc_file):
                    if not modify_cc_file(cc_file, proto_name):
                        return False

                # 在生成的 pb.h 文件中添加 include 语句
                if 'GameBase' in proto_path:
                    h_file_path = os.path.join(dst_path, f"{proto_name}.pb.h")
                    with open(h_file_path, 'r+', encoding='utf-8') as h_file:
                        content = h_file.readlines()
                        # 查找第一个 include 语句的位置
                        for i, line in enumerate(content):
                            if line.startswith('#include'):
                                content.insert(i, '#include "ProtobufExport.h"\n')
                                break
                        else:
                            content.insert(0, '#include "ProtobufExport.h"\n')  # 如果没有找到，插入到文件开头
                        h_file.seek(0)
                        h_file.writelines(content)
                
                log.write("编译成功\n")
                logger.log(f"成功编译: {proto_name} -> {dst_path}", show_console=True)
        
        # 如果错误日志为空，删除它
        if error_log.stat().st_size == 0:
            error_log.unlink()
            
        logger.log("所有proto文件编译完成", show_console=True)
        
        # 复制生成的文件到目标位置
        logger.log("\n开始复制生成的文件到上层目录...", show_console=True)
        if not copy_generated_files():
            return False
            
        # 更新项目文件
        logger.log("\n开始更新项目文件...", show_console=True)
        if not update_vcxproj():
            return False
            
        # 删除临时的dst目录
        try:
            if dst_dir.exists():
                shutil.rmtree(dst_dir)
                logger.log("已清理临时目录", show_console=True)
        except Exception as e:
            logger.log(f"清理临时目录时出错: {str(e)}")
            # 继续执行，不因清理失败而返回错误
            
        logger.log("所有文件处理完成", show_console=True)
        return True
        
    except Exception as e:
        logger.log(f"编译proto文件时出错: {str(e)}", show_console=True)
        return False

class DataTableManagerUpdater:
    def __init__(self, cpp_file_path):
        self.cpp_file_path = os.path.abspath(cpp_file_path)
        self.proto_dir = os.path.abspath('../GameBase/ProtoConfig')
        self.content = []
        self.start_index = -1
        self.end_index = -1
        self.init_start = -1
        self.last_brace_index = -1
        
    def load_file(self):
        """加载并解析文件"""
        try:
            if not os.path.exists(self.cpp_file_path):
                logger.log(f"错误：找不到文件 {self.cpp_file_path}", show_console=True)
                return False
                
            if not os.path.exists(self.proto_dir):
                logger.log(f"错误：找不到目录 {self.proto_dir}", show_console=True)
                return False
                
            # 读取文件内容，使用GBK编码
            with open(self.cpp_file_path, 'r', encoding='GBK', errors='ignore') as f:
                self.content = f.readlines()

            

            # 找到边界
            for i, line in enumerate(self.content):
                if '#include "Tracy.hpp"' in line:
                    self.start_index = i
                elif '#define EXPLICIT_TEMPLATE_INSTANTIATION(T)' in line:
                    self.end_index = i
                    break
                    
            if self.start_index == -1 or self.end_index == -1:
                logger.log("错误：未找到边界标记", show_console=True)
                return False
                
            return True
            
        except Exception as e:
            logger.log(f"加载文件时出错: {str(e)}", show_console=True)
            return False
            
    def update_includes(self):
        """更新include部分"""
        try:
            # 收集所有 .h 文件
            header_files = []
            for file in os.listdir(self.proto_dir):
                if file.endswith('.pb.h'):
                    header_files.append(file)
                    
            logger.log(f"找到 {len(header_files)} 个头文件", show_console=True)
            
            # 构建新的 include 语句
            new_includes = [
                self.content[self.start_index],  # 保留 Tracy.hpp
                "\n"
                "//------------------------------------------------------------------------------\n"
                "// 警告：以下代码由工具自动生成，请勿手动修改\n"
                "//------------------------------------------------------------------------------\n"
            ]
            
            # 按字母顺序添加所有头文件
            for header in sorted(header_files):
                new_includes.append(f'#include "{header}"\n')
                
            new_includes.append(
                "//------------------------------------------------------------------------------\n"
                "// 自动生成代码结束\n"
                "//------------------------------------------------------------------------------\n"
                "\n"
            )
                
            # 组合新的文件内容
            self.content = (
                self.content[:self.start_index] +  # 开始之前的内容
                new_includes +                     # 新的 include 语句
                self.content[self.end_index:]      # 结束之后的内容
            )
            
            return True
            
        except Exception as e:
            logger.log(f"更新include语句时出错: {str(e)}", show_console=True)
            return False
            
    def update_init_function(self):
        """更新Init函数的内容"""
        try:
            # 找到Init函数的开始位置
            init_func_start = -1
            for i, line in enumerate(self.content):
                if 'void MyDataTableMgr::Init()' in line:
                    init_func_start = i
                    break
                    
            if init_func_start == -1:
                logger.log("错误：未找到Init函数", show_console=True)
                return False
            
            # 找到函数的左大括号
            left_brace_index = -1
            for i in range(init_func_start, len(self.content)):
                if '{' in self.content[i]:
                    left_brace_index = i
                    break
                    
            if left_brace_index == -1:
                logger.log("错误：未找到Init函数的左大括号", show_console=True)
                return False
                    
            # 找到ZoneScoped行
            self.init_start = -1
            for i in range(left_brace_index, len(self.content)):
                if 'ZoneScopedN("MyDataTableMgr::Init");' in self.content[i]:
                    self.init_start = i
                    break
                    
            if self.init_start == -1:
                logger.log("错误：未找到Init函数的ZoneScoped标记", show_console=True)
                return False
                    
            # 找到函数的结束位置（从Init函数开始的第一个右大括号）
            init_end = -1
            brace_count = 1  # 从左大括号开始计数
            
            for i in range(left_brace_index + 1, len(self.content)):
                line = self.content[i]
                if '{' in line:
                    brace_count += 1
                if '}' in line:
                    brace_count -= 1
                    if brace_count == 0:  # 找到匹配的右大括号
                        init_end = i
                        break
            
            if init_end == -1:
                logger.log("错误：未找到Init函数的结束位置", show_console=True)
                return False
            
            # 生成新的初始化代码
            init_lines = [
                self.content[left_brace_index],  # 保留左大括号
                self.content[self.init_start],   # 保留ZoneScoped行
                "\n",
                "\t//------------------------------------------------------------------------------\n",
                "\t// 警告：以下代码由工具自动生成，请勿手动修改\n",
                "\t//------------------------------------------------------------------------------\n",
                "\n"
            ]
            
            # 获取所有proto文件并排序
            proto_files = []
            for file in os.listdir(self.proto_dir):
                if file.endswith('.pb.h'):
                    proto_name = file[:-5]  # 移除.pb.h后缀
                    proto_files.append(proto_name)
            
            # 按字母顺序排序
            proto_files.sort()
            
            # 为每个proto文件生成初始化代码
            for proto_name in proto_files:
                init_lines.extend([
                    f"\tDefineRuntimeData<{proto_name}>();\n"
                ])
            
            init_lines.extend([
                "\n",
                "\t//------------------------------------------------------------------------------\n",
                "\t// 自动生成代码结束\n",
                "\t//------------------------------------------------------------------------------\n",
                "\n",
                self.content[init_end]  # 保留右大括号
            ])
            
            # 组合新的文件内容
            self.content = (
                self.content[:init_func_start] +      # Init函数之前的内容
                [self.content[init_func_start]] +     # 函数声明
                init_lines +                          # 新的初始化代码
                self.content[init_end + 1:]           # Init函数之后的内容
            )
            
            return True
            
        except Exception as e:
            logger.log(f"更新Init函数时出错: {str(e)}", show_console=True)
            return False

    def update_template_instantiations(self):
        """更新文件末尾的模板实例化声明"""
        try:
            # 找到最后一个右大括号的位置
            for i, line in enumerate(self.content):
                if '}' in line:
                    self.last_brace_index = i

            if self.last_brace_index == -1:
                logger.log("错误：未找到最后的右大括号", show_console=True)
                return False

            # 获取所有proto文件并排序
            proto_files = []
            for file in os.listdir(self.proto_dir):
                if file.endswith('.pb.h'):
                    proto_name = file[:-5]  # 移除.pb.h后缀
                    proto_files.append(proto_name)
            
            # 按字母顺序排序
            proto_files.sort()

            # 构建新的模板实例化代码
            template_lines = [
                "\n",  # 添加一个空行
                "//------------------------------------------------------------------------------\n",
                "// 警告：以下代码由工具自动生成，请勿手动修改\n",
                "//------------------------------------------------------------------------------\n",
                "\n"
            ]

            # 为每个proto文件生成模板实例化代码
            for proto_name in proto_files:
                template_lines.append(f"EXPLICIT_TEMPLATE_INSTANTIATION({proto_name});\n")

            template_lines.extend([
                "\n",
                "//------------------------------------------------------------------------------\n",
                "// 自动生成代码结束\n",
                "//------------------------------------------------------------------------------\n"
            ])

            # 更新文件内容，保留到最后一个右大括号，然后添加新的内容
            self.content = (
                self.content[:self.last_brace_index + 1] +  # 保留到最后一个右大括号（包含）
                template_lines                              # 添加新的模板实例化代码
            )

            return True

        except Exception as e:
            logger.log(f"更新模板实例化代码时出错: {str(e)}", show_console=True)
            return False

    def save_file(self):
        """保存文件"""
        try:
            # 写回文件，使用GBK编码
            with open(self.cpp_file_path, 'w', encoding='GBK', ) as f:
                f.writelines(self.content)
            return True
        except Exception as e:
            logger.log(f"保存文件时出错: {str(e)}", show_console=True)
            return False

def update_datatable_manager():
    """
    更新 MyDataTableMgr.cpp 文件
    """
    cpp_file = '../GameBase/MyDataTableMgr.cpp'
    logger.log(f"开始更新 MyDataTableMgr.cpp", show_console=True)
    
    updater = DataTableManagerUpdater(cpp_file)
    
    if not updater.load_file():
        return False
        
    if not updater.update_includes():
        return False
        
    if not updater.update_init_function():
        return False
        
    if not updater.update_template_instantiations():
        return False
        
    if not updater.save_file():
        return False
        
    logger.log("成功更新 MyDataTableMgr.cpp", show_console=True)
    return True

def main():
    working_dir = get_working_directory()
    if working_dir:
        logger.log(f"工作目录是：{working_dir}", show_console=True)
        process_files(working_dir)
        logger.log("开始编译proto文件...", show_console=True)
        if compile_proto_files():
            logger.log("开始更新 MyDataTableMgr.cpp...", show_console=True)
            #update_datatable_manager()
    else:
        logger.log("无法获取工作目录", show_console=True)

    input()
        

if __name__ == "__main__":
    main() 