# Hướng dẫn Setup Conveyor & Bucket System

## Cấu trúc Scene

```
Scene
├── SandSimulation (GameObject có Quad/Plane)
│   ├── SandSimulation.cs
│   ├── SandArtGenerator.cs
│   ├── ColorQuantizer.cs
│   └── SandAbsorber.cs
│
├── ConveyorSystem (Empty GameObject)
│   ├── ConveyorGameManager.cs
│   ├── BucketManager.cs
│   │
│   ├── ConveyorBelt (Sprite/Quad)
│   │   ├── ConveyorBelt.cs
│   │   ├── ConveyorStart (Empty - điểm bắt đầu)
│   │   └── ConveyorEnd (Empty - điểm kết thúc)
│   │
│   └── BucketTray (Empty GameObject)
│       ├── BucketTray.cs
│       └── [Slots sẽ tự động tạo]
│
└── Prefabs (trong Assets)
    └── BucketPrefab
        ├── Bucket.cs
        ├── SpriteRenderer (hình xô)
        └── Collider2D (để click)
```

---

## Bước 1: Chuẩn bị Prefab Bucket

### 1.1 Tạo Bucket Prefab
1. Tạo Empty GameObject, đặt tên `BucketPrefab`
2. Thêm components:
   - `SpriteRenderer` (gán sprite hình xô hoặc hình vuông tạm)
   - `BoxCollider2D` (để nhận click)
   - `Bucket.cs`

3. Trong Inspector của Bucket:
   - **Bucket Renderer**: Kéo SpriteRenderer vào
   - **Absorption Radius**: 0.5 (điều chỉnh sau)
   - **Absorption Rate**: 3-5

4. Kéo vào folder `Assets/Prefabs` để tạo prefab

---

## Bước 2: Setup SandSimulation GameObject

### 2.1 Thêm Components
Trên GameObject có SandSimulation, thêm:
- `ColorQuantizer.cs`
- `SandAbsorber.cs`

### 2.2 Cấu hình ColorQuantizer
```
Max Color Groups: 12
Color Threshold: 30-50 (tùy độ chi tiết màu)
Bucket Capacity: 100-200 (tùy kích thước tranh)
Show Debug Log: ✓
```

### 2.3 Cấu hình SandAbsorber
```
Scan Width: 1
Scan Height: 10
Max Absorb Per Frame: 5
Scan Column Above: ✓
Show Debug Gizmos: ✓
```
- **Sand Simulation**: Kéo SandSimulation vào
- **Color Quantizer**: Kéo ColorQuantizer vào
- **Simulation Renderer**: Kéo Renderer của SandSimulation vào

---

## Bước 3: Setup Conveyor Belt

### 3.1 Tạo ConveyorBelt GameObject
1. Tạo Sprite (hoặc Quad) làm hình băng chuyền
2. Đặt vị trí **phía dưới** tranh cát
3. Thêm `ConveyorBelt.cs`

### 3.2 Tạo Start/End Points
1. Tạo 2 Empty GameObject con:
   - `ConveyorStart` - đặt bên **trái** băng chuyền
   - `ConveyorEnd` - đặt bên **phải** băng chuyền

### 3.3 Cấu hình ConveyorBelt
```
Speed: 2-3
Start Point: Kéo ConveyorStart vào
End Point: Kéo ConveyorEnd vào
Sand Simulation: Kéo SandSimulation vào
```

---

## Bước 4: Setup Bucket Tray

### 4.1 Tạo BucketTray GameObject
1. Tạo Empty GameObject, đặt tên `BucketTray`
2. Đặt vị trí **phía dưới** băng chuyền
3. Thêm `BucketTray.cs`

### 4.2 Cấu hình BucketTray
```
Slot Count: 12
Slot Spacing: 1.2 (điều chỉnh theo kích thước xô)
Bucket Prefab: Kéo BucketPrefab vào
Conveyor: Kéo ConveyorBelt vào
Color Quantizer: Kéo ColorQuantizer vào
Sand Absorber: Kéo SandAbsorber vào
Slots Container: Để trống (tự dùng transform này)
Use UI Mode: ☐ (bỏ tick)
```

---

## Bước 5: Setup Game Managers

### 5.1 Tạo ConveyorSystem GameObject
1. Tạo Empty GameObject, đặt tên `ConveyorSystem`
2. Thêm `ConveyorGameManager.cs`
3. Thêm `BucketManager.cs`

### 5.2 Cấu hình ConveyorGameManager
```
Auto Start On Spawn: ✓
Start Delay: 0.5
Start Key: Return (Enter)
Pause Key: P
Reset Key: R
```
- Các references sẽ tự động find, hoặc kéo thủ công

### 5.3 Cấu hình BucketManager
```
Auto Initialize: ✓
Initialize Key: Return
```
- Các references sẽ tự động find

---

## Bước 6: Test

### 6.1 Chạy Game
1. Play scene
2. Nhấn **Space** để spawn tranh cát (từ SandArtGenerator)
3. Nhấn **Enter** để bắt đầu game (hoặc tự động nếu autoStart)
4. **Click vào xô** trong khay để đưa lên băng chuyền
5. Xô sẽ di chuyển và hút cát cùng màu

### 6.2 Debug
- Xem Console log để kiểm tra:
  - ColorQuantizer results (số nhóm màu, số xô)
  - Bucket spawn info
  - Game state changes

---

## Hierarchy mẫu hoàn chỉnh

```
Scene
│
├── Main Camera
│
├── SandCanvas (Quad với SandSimulation)
│   ├── SandSimulation
│   ├── SandArtGenerator
│   ├── ColorQuantizer
│   └── SandAbsorber
│
├── ConveyorBelt (Sprite)
│   ├── ConveyorBelt.cs
│   ├── StartPoint (Empty)
│   └── EndPoint (Empty)
│
├── BucketTray (Empty)
│   └── BucketTray.cs
│
└── GameManager (Empty)
    ├── ConveyorGameManager.cs
    └── BucketManager.cs
```

---

## Điều chỉnh thông số

| Thông số | Mô tả | Giá trị đề xuất |
|----------|-------|-----------------|
| Color Threshold | Độ khác biệt màu để gom nhóm | 30-50 |
| Bucket Capacity | Số pixel mỗi xô hút được | 100-300 |
| Conveyor Speed | Tốc độ băng chuyền | 1-3 |
| Absorption Rate | Số pixel hút mỗi frame | 3-10 |
| Slot Count | Số ô trong khay xô | 12 |

---

## Troubleshooting

### Xô không hút cát
- Kiểm tra SandAbsorber có reference đúng không
- Kiểm tra ColorQuantizer đã analyze chưa
- Tăng Absorption Radius và Absorption Rate

### Màu xô không đúng
- Kiểm tra Color Threshold (giảm nếu muốn chi tiết hơn)
- Chạy ColorQuantizer.TestAnalyze() trong Context Menu

### Xô không di chuyển
- Kiểm tra ConveyorBelt có Start/End Point không
- Kiểm tra Bucket state có chuyển sang OnConveyor không

---

**Lưu ý**: Đảm bảo tất cả references được kéo đúng trong Inspector!
