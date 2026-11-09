# TIME-031 — Hệ thống thời gian trong game

## Mục tiêu

Quản lý thời gian trong game bằng một nguồn thời gian thống nhất, cho thời gian tăng liên tục và hiển thị ngày, giờ hiện tại trên giao diện. Theo yêu cầu trong DOCX, ban đêm kéo dài từ 20:00 đến 07:00.

## Vì sao cần làm

Đồng hồ giao diện, chu kỳ ngày đêm và các tính năng phụ thuộc vào giờ trong game cần dùng cùng một mốc thời gian. Nếu mỗi chức năng tự tính giờ riêng, chúng có thể hiển thị hoặc chuyển trạng thái không đồng nhất.

## File liên quan

- `Assets/Procedural Skybox 2.0 Lite/Materials/DayAndNightCycle.cs` — tính thời gian, cập nhật đồng hồ, xác định ban đêm và cung cấp thời gian cho các hệ thống khác.
- `Assets/Scenes/(PTH)SampleScene 1.unity` — lưu giá trị Inspector của thành phần ngày–đêm trong scene.
- `Assets/Scenes/(PTH)SampleScene (10-11-2026).unity` — lưu giá trị Inspector trong bản scene ngày 10-11-2026.

## Ý tưởng triển khai

1. Dùng một bộ đếm thời gian trong `DayAndNightCycle` làm nguồn giờ duy nhất.
2. Quy đổi thời gian đã trôi qua thành giờ trong ngày và số thứ tự ngày.
3. Hiển thị theo dạng `Day 1 - 6:00`, dùng giờ 24 tiếng.
4. Xác định ban đêm từ 20:00 đến trước 07:00; vì vậy 06:xx vẫn là ban đêm, còn 07:00 đã là ban ngày.
5. Đồng bộ giá trị `Day Start Hour` đã lưu trong scene với mốc 07:00 để giá trị Inspector không ghi đè cấu hình của script.

## Kết quả và kiểm tra

- Đồng hồ tiếp tục chạy và hiển thị ngày, giờ trong game.
- Mốc chuyển từ ban đêm sang ban ngày là 07:00.
- Đã biên dịch `Assembly-CSharp.csproj`; cần kiểm tra trực tiếp trong Unity Play Mode để xác nhận đồng hồ và các chuyển trạng thái tại 06:59/07:00.

## Kịch bản báo cáo

“TIME-031 tạo một nguồn thời gian chung cho game. Script lấy thời gian thực đã trôi qua để tính ngày và giờ, sau đó cập nhật TextMeshPro trên màn hình. Theo yêu cầu, khung giờ từ 20:00 đến trước 07:00 được coi là ban đêm. Giá trị trong scene cũng được chỉnh đồng bộ để Unity không ghi đè mốc giờ bằng cấu hình cũ. Build C# đã thành công; bước kiểm tra cuối là chạy scene trong Unity và quan sát đồng hồ quanh mốc 07:00.”

## Lưu ý còn lại

- Thời lượng một ngày trong game phụ thuộc vào `Cycle Duration` trong Inspector.
- Build C# không thay thế cho việc kiểm tra trực quan trong Unity Play Mode.
