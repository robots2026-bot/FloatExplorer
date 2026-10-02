# Float Explorer

Float Explorer 是面向浮点格式和算子学习的 WinForms 可视化工具。输入一个数值并选择一种格式，可以直接观察位域、实际量化值、舍入误差和相邻可表示值。

## 已支持的格式

- IEEE 754 FP32
- IEEE 754 FP16
- BF16
- TF32 计算舍入视图（不是普通 19 位内存存储类型）
- FP8 E4M3FN（finite-only，最大有限值 448）
- FP8 E5M2

## 主要功能

- 十进制、十六进制原始位串和二进制原始位串输入
- RNE、RZ、RU、RD 四种舍入模式
- 单格式主视图，随时从下拉框切换目标格式
- 彩色 Sign / Exponent / Fraction 位域，单击任意位可翻转
- Normal、Subnormal、Zero、Infinity、NaN 分类与解码公式
- 分区展开 Sign、Exponent、Fraction：显示原始位、Bias 运算、`2^e`、有效数字和最终完整计算公式
- Hex、实际指数、ULP、前后相邻值
- 原始输入与量化结果的绝对误差、相对误差和 ULP 误差
- 最小 Subnormal、最小 Normal、最大有限值等快捷示例

原始位串按当前选中的格式解释，并完整保留输入编码，包括 NaN payload。

## 构建与运行

在 Visual Studio 2026 中打开 `FloatExplorer.slnx`，将 `FloatExplorer` 设为启动项目后运行即可。

命令行：

```powershell
dotnet build FloatExplorer.slnx
dotnet run --project FloatExplorer/FloatExplorer.csproj
```

核心格式测试不依赖第三方测试框架：

```powershell
dotnet run --project FloatExplorer.Tests/FloatExplorer.Tests.csproj
```

## 工程结构

- `FloatExplorer.Core`：格式描述、量化、解码、舍入、比较逻辑
- `FloatExplorer`：WinForms 界面和自绘控件
- `FloatExplorer.Tests`：核心格式的可执行回归测试
