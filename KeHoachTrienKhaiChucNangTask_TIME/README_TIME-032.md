# TIME-032 — Chuyển đổi ánh sáng ngày và đêm

## Mục tiêu

Thay đổi ánh sáng và màu bầu trời theo giờ trong game để người chơi nhận biết ngày, hoàng hôn, ban đêm và bình minh. Việc chuyển đổi cần diễn ra theo các mốc thời gian rõ ràng, đồng thời không làm tối hoặc thay đổi toàn bộ giao diện người dùng.

## Vì sao cần làm

Chu kỳ ánh sáng giúp tạo không khí sinh tồn và cho người chơi biết thời điểm nguy hiểm. Ánh sáng cũng cần dùng chung đồng hồ của TIME-031 để không lệch với giờ hiển thị hoặc cảnh báo ban đêm.

## File liên quan

- `Assets/Procedural Skybox 2.0 Lite/Materials/DayAndNightCycle.cs` — tính vị trí Mặt Trời, nội suy màu bầu trời và cường độ Directional Light.
- `Assets/Scenes/(PTH)SampleScene 1.unity` — lưu các tham số chu kỳ trong Inspector.
- `Assets/Scenes/(PTH)SampleScene (10-11-2026).unity` — lưu các tham số chu kỳ trong bản scene ngày 10-11-2026.

## Ý tưởng triển khai

1. Dùng cùng giờ game với TIME-031.
2. Giữ ban ngày sáng; bắt đầu hoàng hôn từ 17:00 và đạt trạng thái ban đêm lúc 20:00.
3. Bắt đầu chuyển màu bình minh từ 04:00 và kết thúc lúc 07:00.
4. Di chuyển Directional Light theo đường đi từ chân trời lúc 07:00, lên cao giữa ban ngày, rồi về chân trời lúc hoàng hôn.
5. Nội suy màu bầu trời và cường độ ánh sáng theo tiến độ ngày/đêm; không tắt hay đổi màu toàn bộ Canvas.

## Kết quả và kiểm tra

- Chu kỳ ánh sáng và đồng hồ sử dụng cùng nguồn thời gian.
- Cấu hình trong hai scene liên quan đã đặt `Day Start Hour = 7`, khớp với mốc kết thúc ban đêm trong DOCX.
- Build `Assembly-CSharp.csproj` thành công, không có lỗi hoặc cảnh báo.
- Cần quan sát trực tiếp trong Unity Play Mode ở các mốc 04:00, 07:00, 17:00 và 20:00 để đánh giá hình ảnh, độ sáng và khả năng đọc UI.

## Kịch bản báo cáo

“TIME-032 dùng giờ từ TIME-031 để điều khiển Directional Light và màu của Skybox. Bình minh chuyển dần từ 04:00 đến 07:00; Mặt Trời đi từ chân trời lên cao vào ban ngày; hoàng hôn bắt đầu lúc 17:00 và đến 20:00 thì chuyển sang trạng thái ban đêm. Phần UI không bị tắt theo ánh sáng môi trường, còn đồng hồ có màu riêng theo khung giờ nguy hiểm. Build đã thành công; cần kiểm tra trực quan trong Play Mode để tinh chỉnh màu và cường độ phù hợp với scene.”

## Lưu ý còn lại

- Màu sắc và cường độ nhìn đẹp hay không còn phụ thuộc vào Skybox Material, Directional Light, Volume/Exposure và góc nhìn trong scene.
- Build xác nhận mã C# biên dịch được, nhưng không thay cho kiểm thử hình ảnh trong Unity.
