# Tổng quan dự án Survival Game 3D

## 1. Tóm tắt

Đây là dự án game Unity 3D theo hướng sinh tồn góc nhìn thứ nhất. Phần gameplay hiện có các nền tảng: điều khiển player, camera FPS, bắn súng, zombie đuổi và nhận sát thương (3 cấp độ + hệ thống rơi đồ), thanh máu, inventory dạng lưới, shop mua/bán, hệ thống EXP/Level, UI hồ sơ nhân vật, nhặt vật phẩm bằng Raycast và chuyển scene.

Phần lớn hệ thống cơ bản đã kết nối thành vòng lặp gameplay, bao gồm nhiều slot vũ khí, đạn dự trữ trong inventory và HUD vũ khí realtime. Vũ khí cận chiến và lưu trữ tiến trình qua phiên chưa được triển khai; các điểm cần lưu ý được nêu ở mục **Giới hạn và phần chưa hoàn chỉnh**.

## 2. Công nghệ và cấu hình

- Unity **2023.2.22f1**.
- C# trên Unity.
- Unity Input System; cấu hình action nằm ở [Assets/InputSystem_Actions.inputactions](Assets/InputSystem_Actions.inputactions), lớp C# được sinh tự động ở [Assets/Scripts/PlayerInputs.cs](Assets/Scripts/PlayerInputs.cs).
- Có các package Unity cho AI Navigation, UI, URP, HDRP, Animation và những hệ thống khác trong [Packages/manifest.json](Packages/manifest.json).
- Dự án có cấu hình URP trong `Assets/Settings`, đồng thời cũng chứa package và tài nguyên HDRP.
- Tài nguyên âm thanh mới gồm `leberch-survival-horror-263069.mp3` (nhạc nền) và `mohamed_hassan-walking-steps-589527.mp3` (bước chân) trong [Assets/Audio](Assets/Audio); `AudioManager` phát nhạc/hiệu ứng và lưu mức âm lượng bằng `PlayerPrefs`.

## 3. Scene và luồng mở game

### `StartScene`

Scene menu nằm ở [Assets/Scenes/StartScene.unity](Assets/Scenes/StartScene.unity). Các đối tượng có tên cho thấy scene có menu, nút New Game/Load Game/Options/Exit, bảng tùy chọn âm thanh và nút Quit. Scene cũng có `SceneController` và `UIManager`.

### `SampleScene`

Scene gameplay nằm ở [Assets/Scenes/SampleScene.unity](Assets/Scenes/SampleScene.unity). Các đối tượng trong scene cho thấy có Player, Weapon Holder, Zombie, Terrain, camera, InputManager, thanh HP, UI inventory, shop, pause/options và các nút tương tác.

### Luồng scene theo cấu hình hiện tại

- `SceneController` hỗ trợ tải scene theo tên hoặc index, tải lại scene hiện tại, thoát game và hiệu ứng fade nếu được gán CanvasGroup.
- Trong `ProjectSettings/EditorBuildSettings.asset`, các scene được bật trong Build Settings được liệt kê bên dưới.
- `StartScene` và `SampleScene` hiện đều được bật trong Build Settings; `StartScene` đứng trước để làm menu khởi đầu của bản build.

## 4. Player, input và chiến đấu

### Điều khiển và góc nhìn

- `InputManager` tạo và bật bộ input dùng chung.
- `PlayerController` đọc Move và Sprint để di chuyển theo hướng của player bằng `CharacterController`, đổi giữa tốc độ đi bộ và chạy, đồng thời cung cấp trạng thái `IsWalking`/`IsRunning` cho animation.
- Khi có `PlayerStamina`, chạy tiêu hao thể lực và thể lực hồi lại sau một khoảng chờ. `PlayerController` phát hiệu ứng bước chân theo chu kỳ đi/chạy; tham chiếu stamina trong `SampleScene` hiện chưa được gán.
- `FPSCamere` đọc Look, giới hạn góc nhìn dọc và xoay thân player theo chiều ngang. Khi UI mở hoặc cursor đang tự do, input nhìn bị tạm dừng.
- `UIManager` quản lý tập panel UI đang mở và cursor: gameplay khóa/ẩn chuột; UI mở thì thả/hiện chuột. Có thể nhấn Alt để tạm bật/tắt cursor trong gameplay.
- Asset input khai báo Move, Look, Fire, Interact, Crouch, Jump, Previous, Next và Sprint. Các binding chính cho keyboard/mouse là WASD hoặc phím mũi tên, mouse delta, chuột trái, E, C, Space, phím 1/2 và Left Shift. Asset cũng khai báo một số thiết bị khác như gamepad/joystick.
- `InputDebugger` là script hỗ trợ quan sát giá trị di chuyển/chạy.

### Nhặt vật phẩm thế giới

- `PlayerInteraction` raycast từ tâm camera với `RaycastAll`; lọc va chạm trên chính player/phụ huynh của player.
- Khi tia bắn trúng `WorldItem` hợp lệ trong phạm vi `interactDistance`, hiện prompt "[E] Nhặt {tên} x{số lượng}" kèm icon tay.
- Nhấn **E** gọi `InventoryManager.AddItem`; nếu kho đầy thì giữ nguyên vật phẩm ngoài thế giới.
- Khi UI mở hoặc game tạm dừng (`timeScale ≤ 0`), tương tác bị khoá.

### Vũ khí và đổi vũ khí

- `WeaponData`: ScriptableObject cấu hình tên/icon, sát thương, tốc độ bắn, tầm bắn, dung lượng/thời gian nạp đạn, âm thanh/hiệu ứng, cấp độ yêu cầu và loại/ItemData đạn.
- `WeaponController`: Đọc cấu hình từ `WeaponData` (hoặc giá trị fallback Inspector), bắn raycast và nạp đạn theo coroutine. Khi nạp, kiểm tra đạn dự trữ theo ItemData/AmmoType và trừ số đạn thực dùng khỏi `InventoryManager`; chỉ dùng đạn vô hạn khi không cấu hình loại đạn. Không bắn khi đang chạy hoặc nạp đạn.
- `WeaponSwitcher`: Quản lý tối đa `maxWeaponSlots` (mặc định 3), tự bật vũ khí đang chọn và tắt các vũ khí còn lại. Hỗ trợ phím số, cuộn chuột và input actions `Previous`/`Next`; chặn đổi khi UI mở, cursor tự do hoặc đang reload, đồng thời kiểm tra `requiredLevel`. Có thể trang bị item súng từ inventory; slot đầy thì đổi vũ khí đang cầm và đưa vũ khí cũ về inventory.
- `WeaponHUD`: Hiển thị tên và icon súng, đạn hiện có trong băng / đạn dự trữ, cùng cảnh báo nạp đạn, hết đạn hoặc cần nhấn R. HUD được gắn trong `SampleScene` và cập nhật realtime.
- Các cấu hình `WeaponData` nằm trong [Assets/Resources/Weapons](Assets/Resources/Weapons) để `WeaponSwitcher` tự tải theo tên `ItemData` khi trang bị từ Inventory. Mỗi súng có sát thương, tốc độ bắn, tầm bắn, dung lượng băng và thời gian nạp riêng; M1911 giữ sát thương 25, băng 8 viên và dùng `PistolAmmo`.

### Máu, EXP và hệ thống cấp độ

- `PlayerHealth` quản lý HP, đồng bộ `maxHP` từ `PlayerStats`, cập nhật Slider/Text, nhận sát thương/hồi máu. `TakeDamage` có overload cho `int` và `float`; chỉ xử lý chết một lần.
- Khi Player chết, `GameOverManager` hiện panel Game Over, thả con trỏ và tạm dừng thời gian.
- `PlayerLevelSystem` (Singleton) nhận EXP từ `ZombieAI.Die` → xử lý lên cấp liên tiếp → gọi `PlayerStats.AddBaseStatsOnLevelUp` và `PlayerHealth.SyncMaxHealth`. EXP để lên cấp tiếp = expToNextLevel × 1.2.
- `PlayerStats` quản lý coin và 4 chỉ số (MaxHP, Damage, Defense, Agility) ở dạng gốc/hiện tại; hỗ trợ `EquipItem`/`UnequipItem` và kiểm tra `requiredLevel`.

### Zombie (3 cấp độ)

- `ZombieAI` có enum `ZombieType` gồm Normal / Medium / Boss với bộ chỉ số riêng được áp dụng tự động qua `autoApplyTierStats`.
- Zombie di chuyển ngẫu nhiên (Wander) khi player ngoài `detectionRange`; chuyển sang truy đuổi khi trong phạm vi hoặc bị bắn trúng; mất dấu khi player vượt `loseTargetRange`.
- Medium có `callHordeRadius` để gọi đồng bọn; Boss khi HP < 50% vào trạng thái Cuồng Nộ: tăng tốc, tăng sát thương, triệu tập đàn rộng hơn.
- Zombie chết: cộng EXP cho player, kích hoạt hệ thống rơi đồ (`DropLoot`), phát animation rồi bị hủy sau 5 giây.
- Thanh máu zombie (`Zombie_HP_Bar.prefab`) được tạo riêng cho từng con, ghim lên đầu bằng `LateUpdate` chuyển đổi tọa độ 3D → screen. Chỉ hiện khi zombie đã bị bắn trúng; ẩn khi zombie chết hoặc ở sau camera.

## 5. Inventory, vật phẩm và shop

### Dữ liệu vật phẩm

- `ItemData` là ScriptableObject chứa tên, icon, loại vật phẩm (`Consumable`, `Permanent`, `Equipment`), giá, loại trang bị, level yêu cầu, phạm vi, loại đạn, prefab thế giới, chỉ số cộng thêm và giới hạn stack. Mô tả công dụng được ghép từ các thuộc tính này.
- Thư mục [Assets/Items](Assets/Items) hiện có 29 asset `ItemData`, gồm súng, đạn, công cụ và thực phẩm.
- `ItemGenerator` là công cụ trong Unity Editor để tạo `ItemData` từ các ảnh PNG trong `Assets/GeneratedIcons`.
- `PrefabToIconGenerator` là công cụ Editor để render prefab thành icon PNG. Hiện công cụ quét `Assets/ForPlayer/SSG_Guns_Pack_01_Demo/Prefabs`, tạo icon trong `Assets/GeneratedIcons` và prefab vật phẩm thế giới trong `Assets/Prefab`.
- `PlayerStats` quản lý coin cùng các chỉ số gốc/hiện tại; `PlayerLevelSystem` theo dõi EXP và tăng cấp; `PlayerHealth` đồng bộ HP tối đa từ `PlayerStats`. Các script chỉ số có mặt trong project, nhưng inventory hiện chưa có luồng trang bị để áp dụng bonus.
- Commit `d71a2f3` và commit revert `c1f2064` là lịch sử; chúng không mô tả đầy đủ trạng thái mã nguồn hiện tại.

### Inventory

- `InventoryManager` mặc định tạo lưới 35 ô; có hiển thị icon/số lượng, thông tin item đang chọn, nút sử dụng, kéo-thả đổi vị trí/stack và thao tác vứt đồ.
- Đồ cùng loại có thể cộng dồn theo `ItemData.maxStack` khi thêm vào inventory; kéo thả gộp stack vẫn dùng ngưỡng 99 cố định.
- Có `TestAddItem` để nhấn T thêm 5 item mẫu vào inventory khi đã gán tham chiếu trong Inspector.
- Kéo item ra ngoài panel sẽ tạo prefab thế giới (prefab riêng của item hoặc prefab chung), truyền item và số lượng sang `WorldItem`. Item có thể được nhặt khi va chạm Player; phần hút item về Player hiện đang bị comment.

### Shop

- `ShopManager` quản lý danh sách hàng, giá, tồn kho, số coin, chọn số lượng, mua và bán.
- Shop mua lấy một danh sách item ngẫu nhiên từ `Assets/Items`, dùng `ItemData.Price` nếu có giá dương và fallback về giá mặc định; shop bán đồng bộ các item trong inventory và dùng giá bán bằng khoảng 2/3 giá mặc định.
- `ShopUIManager` mở/đóng shop và chuyển giữa panel mua/bán.
- Coin nằm trong `PlayerStats`, mặc định 1000; `ShopManager` đọc/trừ/cộng coin qua component này. Chưa thấy cơ chế lưu coin qua phiên chơi.

## 6. UI, âm thanh và tiện ích

- `UIManager`: quản lý cursor và trạng thái panel UI, đồng thời có hàm tiện ích bật/tắt GameObject, CanvasGroup, cập nhật text và trạng thái button.
- `SceneController`: điều hướng scene, fade tùy chọn và thoát ứng dụng.
- `GameOverManager`: hiển thị panel/câu quote Game Over, tạm dừng gameplay và xử lý restart, về menu hoặc thoát.
- `AudioManager`: nhạc nền, hiệu ứng âm thanh, âm lượng lưu bằng `PlayerPrefs` và duy trì instance khi đổi scene.
- `PlayerStamina`: tiêu hao/hồi thể lực khi chạy và cập nhật UI.
- `PlayerStats`, `PlayerLevelSystem`, `PlayerProfileUI`: chỉ số/coin, tiến trình level/EXP và UI hồ sơ player.
- `WorldItem`: dữ liệu vật phẩm ngoài thế giới và nhặt item vào inventory.
- `VolumeSettings`: đổi mức âm thanh tổng và hiệu ứng thông qua AudioMixer parameter `MasterVolume`/`EffectVolume`.
- `ButtonImageChanger`: đổi sprite của Image khi được gọi.
- `ButtonTextColorChanger`: đổi màu chữ theo trạng thái hover/nhấn.
- `UIEffectPulsing`: tạo hiệu ứng đổi màu nhấp nháy khi đối tượng mục tiêu còn hoạt động; hiệu ứng scale hiện được để comment.

## 7. Danh mục script trong `Assets/Scripts`

| Script | Vai trò |
|---|---|
| `PlayerController.cs` | Di chuyển, đi bộ/chạy, stamina và hiệu ứng bước chân |
| `FPSCamere.cs` | Camera góc nhìn thứ nhất, xoay nhìn |
| `PlayerInputs.cs` | Lớp input C# được Unity Input System sinh tự động |
| `InputManager.cs` | Tạo/bật input dùng chung |
| `InputDebugger.cs` | Công cụ đọc thử input di chuyển/chạy |
| `PlayerHealth.cs` | Máu player/UI, nhận sát thương/hồi máu; đồng bộ maxHP từ PlayerStats |
| `PlayerStats.cs` | Coin và các chỉ số gốc/hiện tại; hỗ trợ EquipItem/UnequipItem |
| `PlayerLevelSystem.cs` | Singleton theo dõi EXP, level và tăng chỉ số khi lên cấp |
| `PlayerProfileUI.cs` | Hiển thị hồ sơ, chỉ số, HP và EXP realtime |
| `PlayerStamina.cs` | Tiêu hao/hồi stamina và cập nhật UI |
| `PlayerInteraction.cs` | Raycast nhặt WorldItem bằng phím E; hiện/ẩn prompt tương tác |
| `GameOverManager.cs` | Màn hình Game Over, câu quote, pause, restart/menu/quit |
| `WeaponData.cs` | ScriptableObject cấu hình súng: sát thương, tốc độ bắn, tầm bắn, băng đạn, âm thanh, icon, loại đạn |
| `WeaponController.cs` | Bắn raycast, đạn, reload, đọc cấu hình từ WeaponData hoặc fallback Inspector |
| `WeaponSwitcher.cs` | Quản lý danh sách vũ khí con trong WeaponHolder, đổi súng bằng phím số/cuộn chuột/Previous-Next, chặn khi reload/UI |
| `WeaponHUD.cs` | Hiển thị tên/icon vũ khí, đạn trong băng/đạn dự trữ và trạng thái vũ khí realtime |
| `ZombieAI.cs` | 3 cấp độ zombie (Normal/Medium/Boss), NavMesh, Wander, Aggro, hệ thống rơi đồ |
| `Target.cs` | Hiệu ứng đổi màu khi bị bắn |
| `InventoryManager.cs` | Inventory, chọn/sử dụng/kéo-thả/vứt item |
| `ItemData.cs` | Dữ liệu item dạng ScriptableObject (hồi máu, sát thương, loại, mô tả, stack) |
| `WorldItem.cs` | Vật phẩm ngoài thế giới và xử lý nhặt vào inventory |
| `ItemGenerator.cs` | Công cụ Editor tạo ItemData từ icon |
| `TestAddItem.cs` | Thêm item thử nghiệm bằng phím T |
| `ShopManager.cs` | Dữ liệu shop, mua/bán, coin và tồn kho |
| `ShopUIManager.cs` | Điều khiển panel mua/bán |
| `UIManager.cs` | Tiện ích quản lý UI |
| `SceneController.cs` | Tải/reload scene, fade và thoát game |
| `AudioManager.cs` | Nhạc nền, hiệu ứng âm thanh và thiết lập âm lượng |
| `VolumeSettings.cs` | Điều chỉnh âm lượng AudioMixer |
| `ButtonImageChanger.cs` | Đổi hình ảnh nút |
| `ButtonTextColorChanger.cs` | Hiệu ứng màu chữ nút |
| `UIEffectPulsing.cs` | Hiệu ứng nhấn mạnh UI |
| `PrefabToIconGenerator.cs` | Công cụ Editor tạo icon từ prefab |

## 8. Tài nguyên nội dung

Dự án chứa nhiều tài nguyên mẫu và gói nội dung bên cạnh code gameplay, gồm:

- Môi trường và vật thể sinh tồn trong `Assets/ForEnviroment`.
- Vũ khí, hiệu ứng chiến đấu trong `Assets/ForPlayer`.
- Zombie và animation liên quan trong `Assets/ForZombies`, `Assets/Animations` và `Assets/Kevin Iglesias`.
- Động vật/mô hình quân sự trong `Assets/ForAnimals`.
- UI bên thứ ba trong `Assets/Adventure UI Kit`, `Assets/Gentleland`, `Assets/MyIndie.Games` và `Assets/kenney_fantasy-ui-borders`.
- Âm thanh trong `Assets/Audio` và `Assets/Sounds`.
- Có các scene demo/sample đi kèm các gói nội dung. Chúng không đồng nghĩa với scene gameplay của game; hai scene chính của project nằm trong `Assets/Scenes`.

## 9. Giới hạn và phần chưa hoàn chỉnh đáng chú ý

Các ghi chú dưới đây được suy ra từ mã nguồn và cấu hình scene/build đã kiểm tra; chưa phải kết quả kiểm thử play mode:

1. **Vị trí component Player đã được hợp nhất chuẩn hóa:** Các component `PlayerHealth`, `PlayerStats`, `PlayerLevelSystem`, `PlayerStamina` đã được gom và gắn trực tiếp trên GameObject gốc `Player` trong `SampleScene`. Các zombie và inventory đã chuyển sang tham chiếu trực tiếp `PlayerHealth` trên Player. Component `PlayerHealth` thừa trên `HP_Bar` đã được dọn dẹp.
2. **Hiệu ứng dùng item chưa hoàn chỉnh:** item hồi máu đang gọi `PlayerHealth.Heal` trong `ApplyItemEffect`; `damageBonus` chưa được áp dụng vào `WeaponController`. Item hồi máu không dùng được khi HP đã đầy.
3. **Hút item bị tắt:** vứt item đã tạo prefab và chuyển dữ liệu sang `WorldItem`, nhưng đoạn hút item về Player đang bị comment trong `WorldItem.Update`. Nhặt bằng Raycast (phím E) qua `PlayerInteraction` vẫn hoạt động bình thường.
4. **Input đã khai báo nhưng chưa thấy logic gameplay tương ứng trong các script chính:** Jump, Crouch, Interact, Previous và Next có action/binding, nhưng chưa được xử lý trong các script gameplay đã rà soát. `PlayerController` hiện chỉ thực hiện dịch chuyển ngang theo input; chưa có trọng lực/nhảy/ngồi.
5. **Khởi tạo hàng mua phụ thuộc Unity Editor:** `ShopManager` và `ZombieAI.EnsureDropPoolLoaded` đều dùng `AssetDatabase` để quét asset, API này chỉ chạy trong Editor. Cần cách cung cấp danh sách item tương thích build runtime.
6. **Stack khi kéo thả chưa dùng giới hạn item:** thêm item vào inventory đã tôn trọng `ItemData.maxStack`, nhưng thao tác kéo thả gộp stack vẫn dùng ngưỡng 99.
7. **Game Over cần gán UI trong Inspector:** component đã có trong `SampleScene`, nhưng cần gán `gameOverPanel` và `quoteText`, sau đó nối các nút panel tới `RestartGame`, `ReturnToMainMenu` và `QuitGame`.
8. **Giới hạn hệ thống vũ khí:** Đổi/trang bị nhiều súng, kiểm tra `requiredLevel`, trừ đạn inventory khi reload và HUD tên/icon/đạn/trạng thái đã được triển khai. Chưa thấy crosshair thích ứng hoặc HUD hiển thị các slot vũ khí; trường hợp không cấu hình loại đạn vẫn cho phép nạp vô hạn.
9. **Coin chưa lưu qua phiên chơi:** `PlayerStats` giữ coin trong bộ nhớ; không có cơ chế `PlayerPrefs` hay serialization để duy trì giá trị qua lần chơi.

## 10. Phạm vi rà soát

Tài liệu này tổng hợp các file C# trong `Assets/Scripts`, asset input, hai scene trong `Assets/Scenes`, Build Settings, cấu hình package và cấu trúc các nhóm tài nguyên chính. Chưa xác minh mọi liên kết Inspector, animation event, collider/layer, AudioMixer, NavMesh bake hay từng prefab trong Unity Editor; vì vậy những nội dung phụ thuộc cấu hình Inspector cần được kiểm tra thêm trực tiếp trong Unity.
