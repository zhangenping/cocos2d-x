# Protobuf配置文件指南

## 1. 快速开始

非静态配置如协议和动态配置直接添加或修改.proto后，双击执行 `ConfigConverter.exe`

### 1.1 创建配置文件步骤

1. 创建一个新的csv文件（参考示例文件：`ini/client/common/datatable/danmu.csv`）

2. 按照以下格式编写配置文件的前三行：
   - 第一行：字段名（用`英文逗号,`分隔，需遵循[字段命名规范](#21-字段命名规范)）
   - 第二行：数据类型（用`英文逗号,`分隔，参考[支持的数据类型](#22-支持的数据类型)）
   - 第三行：字段注释（用`英文逗号,`分隔）

3. 从第四行开始添加具体数据（参考[配置文件示例](#12-配置文件示例)）

4. 第一列字段类型需要为 `T_INDEX` 或 `T_KEY`, 其中 `T_INDEX`表示该表以顺序表存储，`T_KEY`表示该表以散列存储

5. 生成文件：
   - 进入 `src/Proto` 目录
   - 双击执行 `ConfigConverter.exe`
   - 会生成以下文件：
     1. `.proto` 文件：Protobuf的描述文件
     2. `.pb.h` 和 `.pb.cc`：由proto生成的C++代码文件（在[代码使用](#31-读取配置数据)时需要包含）
     3. `.bytes`：二进制配置数据文件，生成在游戏运行目录的 `ini/client/common/databytes` 目录下

### 1.2 配置文件示例

以下是 `danmu.csv` 的示例内容：

id,type,color,ani_title,name,btn_title,eprice,price,effect<br>
T_KEY,T_UINT,T_UINT64,T_ANI,T_LPCSTR,T_ANI,T_UINT,T_UINT,T_EFFECT<br>
ID,类型,颜色,图片Title,名称,按钮Title,魔石,金币,特效<br>
1,0,4294967295,danmu_nordanmupic,免费,danmu_freedanmubtn,0,0,<br>
2,1,4279365358,danmu_bluedanmupic,8,danmu_dolphinbtn,8,0,<br>
3,2,4294967295,danmu_reddanmupic,68,danmu_rocketdanmubtn,68,0,danmu_liaotian<br>

### 1.3 配置热重载

配置文件支持两种热重载方式：

1. 重载所有配置
   ```
   ReloadDataTable
   ```
   - 作用：重新加载所有配置文件
   - 适用场景：修改了多个配置文件，需要批量更新

2. 重载指定配置
   ```
   ReloadDataTable [config_name]
   ```
   - 作用：只重载指定的配置文件
   - 参数：config_name 为配置文件名（不含扩展名）
   - 示例：`ReloadDataTable danmu`
   - 适用场景：只修改了单个配置文件，需要快速生效

注意：配置热重载在游戏运行时即时生效，无需重启游戏。

## 2. 注意事项

### 2.1 字段命名规范

- 必须全部小写
- 多词使用下划线（_）分隔
- 示例：
  - ? 正确：`user_name`, `total_amount`, `is_active`
  - ? 错误：`userName`, `TotalAmount`, `isActive`

### 2.2 支持的数据类型

| 配置文件类型 | C++ 类型 | Protobuf 类型 | 描述 |
|-------------|---------|---------------|------|
| T_UINT      | unsigned int | uint32 | 32位无符号整数 |
| T_INT       | int     | int32         | 32位有符号整数 |
| T_UINT64    | unsigned long long | uint64 | 64位无符号整数 |
| T_INT64     | long long | int64       | 64位有符号整数 |
| T_LPCSTR    | std::string | bytes    | 字符串 |
| T_ANI    	  | std::string | bytes    | 字符串(用于图片title) |
| T_EFFECT    | std::string | bytes    | 字符串(用于光效title) |
| T_SOUND     | std::string | bytes    | 字符串(用于音频路径) |
| T_ATLASSPRITE    | std::string | bytes    | 字符串(用于图集title) |
| T_ARRAY[`INNER_TYPE`]| - | repeated `INNER_TYPE` | `INNER_TYPE`对应的数组,目前只支持T_INT/T_UINT/T_INT64/T_UINT64

## 3. 代码使用示例

### 3.1 读取配置数据

#### 3.1.1 读取散列表配置数据

配置数据可以通过 `MyDataTableMgr` 单例直接访问。使用前请确保：
1. 配置文件已正确生成（参考[创建配置文件步骤](#11-创建配置文件步骤)）
2. `MyDataTableMgr.cpp` 中的 `Init` 函数添加对应的 `DefineRuntimeData`，文件结尾处添加对应的 `EXPLICIT_TEMPLATE_INSTANTIATION`
3. 如需热重载，请参考[配置热重载](#13-配置热重载)章节

```cpp
#include "MyDataTableMgr.h"
#include "magictype.pb.h"  // Protobuf生成的头文件

// 示例：通过ID获取配置
const DT_magictype* pMagicType = nullptr;
if (MyDataTableMgr::GetInstance().TryGetByID<magictype>(dwTypeAndLevel, pMagicType)) {
    // 成功获取配置数据
    // 可以通过 pMagicType 访问具体字段
} else {
    // 未找到对应配置
}
```

参数说明：
- `magictype`: Protobuf生成的Message类型（需要包含对应的 .pb.h 文件）
- `dwTypeAndLevel`: 配置表中的键值（对应第一列ID）
- `pMagicType`: const指针输出参数，指向配置数据

# 3.1.2 读取顺序表配置数据

```cpp
// 示例：顺序表通过序号获取配置
const DT_example* pExample = nullptr;
if (MyDataTableMgr::GetInstance().TryGetByIndex<example>(0, pExample)) {
    // 成功获取配置数据
    // 可以通过 pExample 访问具体字段
} else {
    // 未找到对应配置
}
```

参数说明：
- `example`: Protobuf生成的Message类型（需要包含对应的 .pb.h 文件）
- `pExample`: const指针输出参数，指向配置数据

注意事项：
- 确保包含了正确的 Protobuf 生成的头文件
- 确保在使用前已正确加载配置文件
- 返回值为 bool 类型，表示是否成功获取数据
- 返回的指针指向只读数据，不可修改
- 顺序表若需要根据ID获取数据，建议使用ForEach实现