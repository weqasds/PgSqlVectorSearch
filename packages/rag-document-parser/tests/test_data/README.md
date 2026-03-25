# 测试数据

此目录包含用于测试的文档文件。

## 文件要求

为了进行有效的测试，建议包含以下类型的文件：

### 1. PDF文件
- `simple.pdf` - 简单的文本PDF（1-2页）
- `with_tables.pdf` - 包含表格的PDF
- `with_images.pdf` - 包含图像的PDF
- `scanned.pdf` - 扫描版PDF（需要OCR）

### 2. 图像文件
- `text_image.png` - 包含清晰文本的PNG图像
- `text_image.jpg` - 包含清晰文本的JPEG图像
- `multi_language.png` - 包含多语言文本的图像

### 3. Office文档
- `simple.docx` - 简单的Word文档
- `with_tables.docx` - 包含表格的Word文档
- `simple.xlsx` - 简单的Excel文档
- `with_formulas.xlsx` - 包含公式的Excel文档
- `simple.pptx` - 简单的PowerPoint文档

### 4. 其他格式
- `plain.txt` - 纯文本文件
- `complex.pdf` - 复杂PDF（多页、多种元素）

## 文件大小
- 小文件：< 100KB（用于快速测试）
- 中文件：100KB - 1MB（用于一般测试）
- 大文件：1MB - 10MB（用于性能测试）

## 注意事项
1. 确保测试文件不包含敏感信息
2. 使用开源或自创的测试文件
3. 文件应具有代表性，覆盖常见用例
4. 考虑跨平台兼容性（避免使用特定于操作系统的格式）

## 自动生成
某些测试文件可以在测试运行时动态生成（如简单的PDF、图像）。
对于复杂的测试用例，建议使用预先准备好的真实文档。

## 版权
确保所有测试文件不侵犯版权。建议使用：
- 自己创建的文档
- 开源测试文件
- 明确允许用于测试的文档