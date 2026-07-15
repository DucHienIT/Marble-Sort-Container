# Truck Ball Jam — Game Design & Technical Specification

> Tài liệu mô tả luật chơi, mechanic, giao diện và kiến trúc kỹ thuật của game **Truck Ball Jam**, dùng làm brief để **Claude Code CLI** triển khai prototype bằng Unity.

---

## 1. Tổng quan game

- **Thể loại:** Casual puzzle / color sorting / conveyor jam.
- **Nền tảng:** Mobile portrait.
- **Engine:** Unity.
- **Source Object:** Xe tải hoặc container truck.
- **Số bi mặc định mỗi xe:** `12`.
- **Tương tác chính:** Chạm vào một xe tải → cửa sau mở → toàn bộ 12 viên bi bị dồn bên trong cùng lúc tràn ra → bi tự động trượt qua máng xuống băng chuyền.
- **Mục tiêu:** Chọn đúng thứ tự mở xe để các viên bi được chuyển vào hộp đích cùng màu mà không làm đầy băng chuyền.
- **Điểm nhấn hình ảnh:** Các viên bi phải có cảm giác bị nhồi chặt, chen lấn và dồn ứ trong thùng xe trước khi cùng lúc tràn ra ngoài.

### Core loop

```text
Quan sát màu bi trong các xe
→ Chọn một xe để mở
→ 12 viên bi cùng lúc tràn ra
→ Bi trượt xuống máng
→ Bi nhập vào băng chuyền
→ Hộp đích hút bi cùng màu
→ Xe rỗng rời khỏi màn chơi
→ Tiếp tục cho đến khi xử lý hết tất cả xe
```

---

## 2. Thành phần màn hình

Bố cục dọc từ trên xuống dưới:

### 2.1 HUD

- Nút Pause.
- Level hiện tại.
- Tiến trình, ví dụ: `24/48`.
- Có thể hiển thị số slot trống của băng chuyền.
- MVP không bắt buộc có timer.

### 2.2 Khu vực xe tải

- Hiển thị từ 3 đến 6 xe.
- Mỗi xe chứa mặc định 12 viên bi.
- Bi có thể gồm nhiều màu trộn lẫn.
- Người chơi có thể nhìn thấy màu và số lượng bi trong xe trước khi chọn.
- Xe chưa mở có thể rung nhẹ và phát âm thanh bi va chạm bên trong.

### 2.3 Cửa sau và máng trượt

- Cửa sau xe mở xuống dưới hoặc sang hai bên.
- Sau cửa xe là một máng nghiêng dẫn bi xuống băng chuyền.
- Máng cần đủ rộng để 12 viên bi chen nhau trượt xuống.
- Có thành chắn để bi không rơi khỏi đường đi.

### 2.4 Băng chuyền

- Là vùng lưu tạm có giới hạn.
- Bi chạy theo một đường cố định, có thể là:
  - Đường thẳng.
  - Hình chữ U.
  - Đường vòng.
- Băng chuyền được chia thành các slot logic.
- Mỗi viên bi chiếm 1 slot.
- Nếu không còn đủ slot để nhận bi mới, người chơi thua.

### 2.5 Hộp đích

- Mỗi hộp có một màu.
- Hộp chỉ nhận viên bi cùng màu.
- Khi viên bi đi ngang qua hộp cùng màu, hộp tự động hút viên bi vào.
- Khi hộp đầy, hộp hoàn thành và được thay bằng hộp tiếp theo trong queue.

---

## 3. Luật chơi chi tiết

### 3.1 Bắt đầu level

1. Sinh danh sách xe tải theo dữ liệu level.
2. Mỗi xe được gán danh sách 12 màu bi.
3. Sinh các hộp đích đang active.
4. Khởi tạo băng chuyền rỗng.
5. Cho phép người chơi chọn xe.

### 3.2 Khi người chơi chạm vào xe

1. Kiểm tra xe có đang ở trạng thái sẵn sàng không.
2. Khóa tương tác với xe đó.
3. Xe rung nhẹ.
4. Cửa sau bắt đầu mở.
5. Các viên bi bên trong dồn về phía cửa.
6. Giữ lại trong khoảng `0.15–0.30 giây` để tạo cảm giác bị kẹt.
7. Giải phóng toàn bộ 12 viên bi cùng một lần.
8. Bi chen nhau trượt xuống máng.
9. Khi bi đi vào Intake Zone, chuyển bi sang logic của băng chuyền.
10. Khi xe hết bi, xe đóng cửa và rời khỏi màn chơi.

### 3.3 Xử lý bi trên băng chuyền

Với mỗi viên bi:

- Nếu đi tới vùng hút của hộp cùng màu:
  - Bi rời khỏi băng chuyền.
  - Bi bay hoặc lăn vào hộp.
  - Tăng số bi trong hộp.
- Nếu chưa gặp hộp cùng màu:
  - Bi tiếp tục chạy trên băng chuyền.
- Nếu hộp đầy:
  - Hộp chạy animation hoàn thành.
  - Hộp mới trong queue được đưa vào vị trí active.

### 3.4 Điều kiện thắng

Người chơi thắng khi:

```text
Tất cả xe đã mở
AND
Tất cả xe đã hết bi
AND
Không còn bi trên máng trượt
AND
Không còn bi trên băng chuyền
AND
Tất cả bi đã được đưa vào hộp đích
```

### 3.5 Điều kiện thua

Người chơi thua khi:

```text
Số bi cần nhập vào băng chuyền
>
Số slot trống còn lại
```

MVP chỉ sử dụng điều kiện thua do băng chuyền đầy.

### 3.6 Chiến thuật cốt lõi

- Mỗi lần chạm là một quyết định lớn vì xe đổ toàn bộ 12 viên.
- Người chơi phải quan sát:
  - Màu bi trong xe.
  - Hộp đích đang active.
  - Số slot trống trên băng chuyền.
  - Thứ tự hộp tiếp theo.
- Mở sai xe có thể làm 12 viên bi không được xử lý kịp và gây tắc băng chuyền.

---

## 4. Cảm giác dồn ứ trong container

Đây là yêu cầu hình ảnh quan trọng nhất của mechanic.

### 4.1 Trạng thái trước khi mở

- 12 viên bi nằm trong một khoang hơi chật.
- Bi không xếp thành hàng quá hoàn hảo.
- Một số viên nằm cao hơn hoặc chèn lên viên khác.
- Các viên sát cửa phải tạo cảm giác đang ép vào cửa.
- Xe có idle animation rung rất nhẹ.
- Phát âm thanh `lọc cọc` nhỏ khi xe rung.

### 4.2 Khi cửa mở

- Cửa không làm bi rơi ra ngay lập tức.
- Các viên phía trước nhúc nhích nhưng vẫn bị kẹt.
- Các viên phía sau đẩy dồn lên.
- Sau một khoảng delay ngắn, cả cụm bi cùng lúc tràn ra.
- Có thể để 1 viên cuối bị mắc lại rồi rơi ra chậm hơn một chút.

### 4.3 Khi bi trượt xuống

- Bi va chạm với nhau.
- Bi có tốc độ hơi khác nhau.
- Một số viên bị chèn rồi bật sang bên cạnh.
- Bi lăn vào thành máng nhưng không được rơi khỏi máng.
- Dòng bi phải có cảm giác giống một khối vật thể vừa được giải phóng.

### 4.4 Nguyên tắc kỹ thuật

- Dùng physics cho phần trình diễn.
- Dùng slot logic cho gameplay.
- Không để kết quả level phụ thuộc hoàn toàn vào Rigidbody.
- Khi bi tới Intake Zone, chuyển từ physics tự do sang điều khiển theo slot băng chuyền.

---

## 5. Đặc tả hệ thống

### 5.1 Truck Source

```text
TruckSource {
  id: string
  ballColors: BallColor[12]
  state: "ready" | "opening" | "jammed" | "releasing" | "empty" | "leaving"
  canTap: bool
  doorOpenDuration: float
  jamHoldDuration: float
}
```

Trạng thái của xe:

```text
Ready
→ Opening
→ Jammed
→ Releasing
→ Empty
→ Leaving
```

### 5.2 Ball

```text
Ball {
  id: string
  color: BallColor
  state:
    "inside_truck"
    | "sliding"
    | "waiting_intake"
    | "on_conveyor"
    | "moving_to_destination"
    | "completed"
}
```

### 5.3 Conveyor

```text
Conveyor {
  capacity: int
  occupiedSlots: int
  balls: Ball[]
  speed: float
}
```

Quy tắc:

```text
freeSlots = capacity - occupiedSlots
```

Khi một bi được đưa vào hộp đích:

```text
occupiedSlots -= 1
```

### 5.4 Destination Box

```text
DestinationBox {
  id: string
  color: BallColor
  capacity: int
  filled: int
  state: "active" | "completed"
}
```

Khi:

```text
filled == capacity
```

thì hộp hoàn thành và hộp tiếp theo trong queue được đưa lên.

### 5.5 Level State

```text
LevelState {
  levelId: int
  totalBalls: int
  completedBalls: int
  remainingTrucks: int
  status: "playing" | "won" | "lost_overflow"
}
```

---

## 6. Kiến trúc Unity đề xuất

### 6.1 Script chính

```text
Assets/Scripts/
├── Core/
│   ├── GameManager.cs
│   ├── LevelManager.cs
│   └── GameState.cs
├── Truck/
│   ├── TruckSource.cs
│   ├── TruckDoor.cs
│   └── TruckBallHolder.cs
├── Ball/
│   ├── BallController.cs
│   ├── BallPhysicsController.cs
│   └── BallPool.cs
├── Conveyor/
│   ├── ConveyorController.cs
│   ├── ConveyorSlot.cs
│   └── ConveyorIntake.cs
├── Destination/
│   ├── DestinationBox.cs
│   └── DestinationManager.cs
└── Data/
    ├── LevelData.cs
    └── TruckData.cs
```

### 6.2 Prefab chính

```text
Assets/Prefabs/
├── Truck.prefab
├── Ball.prefab
├── Conveyor.prefab
├── ConveyorSlot.prefab
├── DestinationBox.prefab
└── LevelRoot.prefab
```

### 6.3 Thành phần Truck Prefab

```text
Truck
├── Visual
├── CargoContainer
│   ├── BallSpawnPoints
│   └── ContainmentColliders
├── RearDoor
├── Ramp
├── ReleaseTrigger
└── ExitPoint
```

### 6.4 Ball Physics

Trong giai đoạn ở xe và máng:

- `Rigidbody` bật.
- `Collider` bật.
- Sử dụng Continuous Collision Detection nếu bi chạy nhanh.
- Giới hạn velocity tối đa để tránh bi bắn khỏi màn hình.

Khi vào conveyor:

- Tắt hoặc chuyển Rigidbody sang Kinematic.
- Đưa bi vào slot trống.
- Cho bi di chuyển theo path hoặc conveyor slot.

---

## 7. Dữ liệu level mẫu

```json
{
  "levelId": 1,
  "conveyorCapacity": 18,
  "destinationCapacity": 3,
  "activeDestinationCount": 4,
  "trucks": [
    {
      "id": "truck_01",
      "balls": [
        "red", "red", "red",
        "blue", "blue", "blue",
        "green", "green", "green",
        "yellow", "yellow", "yellow"
      ]
    },
    {
      "id": "truck_02",
      "balls": [
        "red", "blue", "green", "yellow",
        "red", "blue", "green", "yellow",
        "red", "blue", "green", "yellow"
      ]
    },
    {
      "id": "truck_03",
      "balls": [
        "red", "red", "blue", "blue",
        "green", "green", "yellow", "yellow",
        "red", "blue", "green", "yellow"
      ]
    }
  ],
  "destinationQueue": [
    { "color": "red", "capacity": 3 },
    { "color": "blue", "capacity": 3 },
    { "color": "green", "capacity": 3 },
    { "color": "yellow", "capacity": 3 }
  ]
}
```

Lưu ý: Tổng capacity của toàn bộ destination queue theo từng màu phải khớp với tổng số bi của màu đó trong level.

---

## 8. Thông số khởi đầu để tuning

| Thuộc tính | Giá trị đề xuất |
|---|---:|
| Số bi mỗi xe | 12 |
| Số xe level đầu | 3 |
| Số màu level đầu | 3–4 |
| Conveyor capacity | 18–24 |
| Destination capacity | 3 |
| Thời gian mở cửa | 0.25–0.40 giây |
| Thời gian giữ cảm giác kẹt | 0.15–0.30 giây |
| Thời gian toàn bộ bi tràn ra | 0.60–1.20 giây |
| Thời gian bi trượt qua máng | 0.80–1.50 giây |
| Số destination active | 3–4 |

Các giá trị này chỉ là mặc định ban đầu và cần điều chỉnh qua playtest.

---

## 9. Thứ tự triển khai cho Claude Code CLI

1. Đọc cấu trúc project Unity hiện tại.
2. Tận dụng architecture và coding convention đang có.
3. Tạo data model cho level, truck, ball, conveyor và destination.
4. Dựng một Truck prefab chứa 12 viên bi.
5. Implement tap xe và state machine mở cửa.
6. Implement physics cho bi trong xe và máng.
7. Implement Intake Zone chuyển bi sang conveyor.
8. Implement conveyor slot system.
9. Implement destination box hút bi cùng màu.
10. Implement destination queue.
11. Implement Win/Lose condition.
12. Thêm Object Pooling cho Ball.
13. Thêm debug UI:
    - Số bi trên conveyor.
    - Số slot trống.
    - State của truck.
    - Số bi chưa xử lý.
14. Tạo ít nhất 3 level test.
15. Polish animation, sound và particle sau khi gameplay logic ổn định.

---

## 10. Acceptance Criteria

Prototype được xem là hoàn thành khi:

- Mỗi xe chứa đúng 12 viên bi theo dữ liệu level.
- Người chơi chỉ cần tap một lần để mở xe.
- Toàn bộ 12 viên được giải phóng trong cùng một lượt.
- Bi có cảm giác bị dồn và kẹt trước khi tràn ra.
- Bi tự động trượt xuống máng.
- Không có bi xuyên collider hoặc rơi khỏi khu vực chơi trong điều kiện bình thường.
- Mỗi bi được đưa vào đúng một conveyor slot.
- Hộp đích chỉ nhận bi cùng màu.
- Hộp đầy sẽ được thay bằng hộp tiếp theo.
- Level thắng khi tất cả bi được xử lý.
- Level thua khi conveyor không còn đủ capacity.
- Restart level không làm nhân đôi object hoặc event listener.
- Không có lỗi mất bi, nhân đôi bi hoặc sai tổng số bi.
- Có thể thay đổi dữ liệu level mà không sửa code gameplay.

---

## 11. Yêu cầu khi Claude Code CLI thực hiện

- Đọc toàn bộ tài liệu này trước khi sửa code.
- Kiểm tra project hiện tại trước khi tạo hệ thống mới.
- Giữ code đơn giản, dễ debug và dễ mở rộng.
- Ưu tiên gameplay chạy đúng trước khi polish.
- Không tự ý thay đổi architecture lớn nếu không cần thiết.
- Không tự động thực hiện bất kỳ thao tác Git nào.
- Sau mỗi bước, báo cáo:
  - File đã tạo.
  - File đã sửa.
  - Chức năng đã hoàn thành.
  - Vấn đề còn tồn tại.

Prompt gợi ý:

```text
Read TRUCK_BALL_JAM_GAME_SPEC.md completely, inspect the current Unity project, and implement the mechanic step by step.

Follow the existing project architecture where possible.
Start with data models and one playable prototype level.
Do not perform any Git operations.
After each implementation step, report the files created or modified and any remaining issues.
```

---

## 12. Ghi chú bổ sung

- Physics chỉ dùng để tạo cảm giác vui nhộn trong đoạn bi rời xe và trượt xuống máng.
- Conveyor phải dùng logic xác định để tránh gameplay phụ thuộc vào kết quả vật lý ngẫu nhiên.
- Cần ưu tiên cảm giác `packed → jammed → released`.
- Không nên để bi xuất hiện trực tiếp trên conveyor ngay khi tap xe.
- Máng trượt là một phần bắt buộc của visual flow.
- Phiên bản MVP chưa cần timer, booster, xe đặc biệt hoặc bi đặc biệt.
