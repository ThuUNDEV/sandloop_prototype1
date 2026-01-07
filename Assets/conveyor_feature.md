# Tính năng mới: Conveyor & Bucket System

## Tổng quan
Hệ thống băng chuyền kết hợp với khay đựng xô để tạo trải nghiệm tương tác với tranh cát.

---

## 1. Băng chuyền (Conveyor Belt)

### Vị trí
- Đặt phía dưới tấm ảnh cát

### Chức năng
- Di chuyển cát liên tục từ **trái sang phải**
- Vận chuyển xô khi được đặt lên

---

## 2. Khay đựng xô (Bucket Tray)

### Vị trí
- Nằm phía dưới băng chuyền

### Cấu trúc
- Chia làm **12 ô** để chứa xô

### Chức năng
- Chứa các xô cát
- Mỗi xô có màu tương ứng với màu có trong tranh cát

---

## 3. Xô (Bucket)

### Thuộc tính
- Màu sắc: Lấy từ các màu có trong tranh cát

### Hệ thống màu sắc (Color Quantization)

#### Vấn đề
- Tranh cát có thể có hàng trăm màu khác nhau (nhiều gam màu hồng, xanh...)
- Không muốn tạo quá nhiều xô cho từng shade màu

#### Giải pháp
- **Gom nhóm màu tương tự** (Color Clustering)
- Các màu có gam gần giống nhau → gộp thành 1 xô
- Ví dụ: Hồng nhạt, hồng đậm, hồng cam → 1 xô "Hồng"

#### Thuật toán đề xuất
- Sử dụng **K-Means Clustering** hoặc **Median Cut** để giảm số màu
- Hoặc chia theo **HSV/HSL** (Hue ranges) để gom màu cùng tông
- Số lượng nhóm màu = số lượng xô (tối đa 12 ô)

### Dung lượng xô

#### Tính toán
- Đếm tổng số pixel của mỗi nhóm màu trong tranh
- **1 xô có thể hút nhiều pixel** cùng nhóm màu
- Số lượng xô mỗi màu = Tổng pixel nhóm màu ÷ Dung lượng xô

#### Mục tiêu
- Tổng số xô (tất cả màu) **vừa đủ** để hút hết cát từ tranh
- Phân bổ số xô theo tỷ lệ pixel của mỗi nhóm màu

### Tương tác
- Khi **nhấn vào xô** → Xô được đưa lên băng chuyền

### Cơ chế hút cát
- Khi xô di chuyển trên băng chuyền (trái → phải)
- Xô sẽ **hút cát** từ tranh phía trên (cát cùng màu với xô)
- Cát bị hút sẽ **biến mất** khỏi tranh cát

### Trạng thái xô trên băng chuyền

#### Khi xô ĐẦY cát
- Xô **biến mất** (SetActive = false)
- Giải phóng vị trí trên băng chuyền

#### Khi xô CHƯA đầy + đến cuối băng chuyền
- Xô **quay lại từ đầu** băng chuyền (loop)
- Tiếp tục hút cát cho đến khi đầy

---

## Luồng hoạt động

```
[Tranh cát] ← Cát bị hút biến mất
    ↓ (xô hút cát cùng màu)
[Băng chuyền] ← Xô di chuyển từ trái sang phải
    ↓
[Khay đựng xô - 12 ô] ← Nhấn để chọn xô
```

---

## TODO

- [ ] Thiết kế UI cho khay đựng xô (12 ô)
- [ ] Logic đặt xô lên băng chuyền
- [ ] **Color Quantization**: Thuật toán gom nhóm màu tương tự
- [ ] Tính toán dung lượng xô và số lượng xô mỗi màu
- [ ] Hệ thống xô hút cát cùng nhóm màu
- [ ] Animation di chuyển xô trên băng chuyền
- [ ] Xử lý cát biến mất khi bị hút

---

**Created**: 2026-01-07
