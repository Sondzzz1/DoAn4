"""ISSUE-05: verify the PDF fixture exported by Issue04_pdf_renders_with_bundled_font."""
import re
import sys

import pdfplumber

with pdfplumber.open(sys.argv[1]) as pdf:
    text = "\n".join(page.extract_text() or "" for page in pdf.pages)

checks = {
    "Tiền đặt cọc đã ghi nhận": 1000000,
    "Đơn giá điện": 3500,
    "Đơn giá nước": 20000,
    "Phí dịch vụ khác": 0,
}
for label, expected in checks.items():
    line = next(line for line in text.splitlines() if label in line)
    amount = re.search(r":\s*([\d.,]+)\s*VNĐ", line)
    assert amount, line
    assert int(re.sub(r"\D", "", amount.group(1))) == expected, line
print("PASS ISSUE-05: actual PDF uses all four contract snapshots, not mutable Room fees.")
