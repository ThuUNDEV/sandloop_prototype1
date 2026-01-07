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

## Kế hoạch phát triển (Development Plan)

### Phase 1: Data & Core Systems

#### 1.1 Color Quantization System
**File:** `ColorQuantizer.cs`
- [ ] Phân tích tranh cát, đếm tất cả màu pixel
- [ ] Implement thuật toán gom nhóm màu (K-Means hoặc HSV-based)
- [ ] Output: Danh sách nhóm màu + số pixel mỗi nhóm
- [ ] Tính toán số xô cần thiết cho mỗi nhóm màu

#### 1.2 Bucket Data
**File:** `BucketData.cs`
```csharp
public class BucketData
{
    public Color32 bucketColor;      // Màu đại diện nhóm
    public int capacity;             // Dung lượng tối đa
    public int currentFill;          // Số cát đã hút
    public bool IsFull => currentFill >= capacity;
}
```

---

### Phase 2: Game Objects & UI

#### 2.1 Conveyor Belt
**File:** `ConveyorBelt.cs`
- [ ] Tạo GameObject băng chuyền (Sprite/UI Image)
- [ ] Định nghĩa điểm bắt đầu (trái) và kết thúc (phải)
- [ ] Logic di chuyển xô với tốc độ cố định
- [ ] Xử lý loop: Xô chưa đầy → quay về đầu

#### 2.2 Bucket Tray UI
**File:** `BucketTray.cs`
- [ ] Tạo UI Panel với Grid Layout (12 ô)
- [ ] Spawn xô vào các ô dựa trên ColorQuantizer
- [ ] Xử lý click event cho từng xô

#### 2.3 Bucket GameObject
**File:** `Bucket.cs`
- [ ] Prefab xô với SpriteRenderer (màu dynamic)
- [ ] Thuộc tính: BucketData reference
- [ ] States: InTray, OnConveyor, Completed
- [ ] Visual feedback khi đang hút cát (fill indicator)

---

### Phase 3: Core Mechanics

#### 3.1 Bucket Spawning
**File:** `BucketManager.cs`
- [ ] Nhận data từ ColorQuantizer
- [ ] Tạo số lượng xô chính xác cho mỗi nhóm màu
- [ ] Phân bổ xô vào khay (12 ô, có thể scroll nếu > 12)

#### 3.2 Sand Absorption System
**File:** `SandAbsorber.cs`
- [ ] Khi xô di chuyển, scan cột pixel phía trên
- [ ] So sánh màu pixel với nhóm màu của xô
- [ ] Nếu match: Xóa pixel khỏi tranh + tăng currentFill
- [ ] Tích hợp với SandSimulation (set cell = Air)

#### 3.3 Conveyor Logic
**File:** `ConveyorBelt.cs` (mở rộng)
- [ ] Track danh sách xô đang trên băng chuyền
- [ ] Update vị trí xô mỗi frame
- [ ] Check xô đầy → SetActive(false)
- [ ] Check xô cuối băng chuyền → Reset vị trí

---

### Phase 4: Integration

#### 4.1 Game Flow
**File:** `ConveyorGameManager.cs`
- [ ] Khởi tạo: Load tranh → ColorQuantizer → Spawn xô
- [ ] Gameplay loop: Click xô → Di chuyển → Hút cát
- [ ] Win condition: Tất cả cát bị hút hết

#### 4.2 Integration với SandSimulation
- [ ] Hook vào hệ thống Cell hiện tại
- [ ] Xóa cell bằng cách set type = Air
- [ ] Đảm bảo không conflict với physics job

---

### Phase 5: Polish

#### 5.1 Visual Effects
- [ ] Animation xô di chuyển smooth
- [ ] Particle effect khi hút cát
- [ ] Fill indicator trên xô (progress bar)
- [ ] Conveyor belt animation (texture scrolling)

#### 5.2 Audio (Optional)
- [ ] Sound effect hút cát
- [ ] Sound effect xô đầy/biến mất

---

## Thứ tự triển khai đề xuất

| Bước | Task | Độ ưu tiên |
|------|------|-----------|
| 1 | ColorQuantizer.cs | 🔴 High |
| 2 | BucketData.cs | 🔴 High |
| 3 | ConveyorBelt.cs (basic) | 🔴 High |
| 4 | Bucket.cs + Prefab | 🔴 High |
| 5 | BucketTray.cs (UI) | 🟡 Medium |
| 6 | SandAbsorber.cs | 🔴 High |
| 7 | BucketManager.cs | 🟡 Medium |
| 8 | ConveyorGameManager.cs | 🟡 Medium |
| 9 | Visual Effects | 🟢 Low |
| 10 | Audio | 🟢 Low |

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
