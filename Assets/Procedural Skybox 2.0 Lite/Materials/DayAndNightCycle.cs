using System.Collections; // Dùng IEnumerator và Coroutine để tạo hiệu ứng nhấp nháy cảnh báo.
using UnityEngine; // Dùng các kiểu dữ liệu và hàm cơ bản của Unity.
using TMPro; // Dùng TextMeshProUGUI để điều khiển chữ trên giao diện.

public class DayAndNightCycle : MonoBehaviour // Quản lý thời gian, ánh sáng, bầu trời và cảnh báo ngày đêm.
{ // Bắt đầu phần nội dung của lớp DayAndNightCycle.
    public static DayAndNightCycle Instance { get; private set; } // Cho phép hệ thống khác truy cập bộ điều khiển ngày đêm hiện tại.

    [Header("General Settings")] // Tạo tiêu đề nhóm cài đặt chung trong Inspector.
    public Light sun; // Nhận Directional Light đại diện cho Mặt Trời.
    public Material skyboxMaterial; // Nhận Material của bầu trời để đổi màu theo thời gian.
    [Min(0.01f)] public float cycleDuration = 1440f; // Số giây thực tương ứng với một ngày đầy đủ trong game.
    [Range(0f, 23.99f)] public float startHour = 6f; // Giờ trong game tại thời điểm bắt đầu chạy chu kỳ.

    [Header("UI Settings")] // Tạo tiêu đề nhóm cài đặt giao diện đồng hồ.
    public TextMeshProUGUI timeTextUI; // Nhận TextMeshPro hiển thị ngày và giờ.
    public Color normalClockColor = Color.white; // Đặt màu đồng hồ vào ban ngày.
    public Color nightClockColor = Color.red; // Đặt màu đồng hồ trong khoảng giờ nguy hiểm.

    [Header("Night Warning")] // Tạo tiêu đề nhóm cài đặt cảnh báo ban đêm.
    public TextMeshProUGUI warningTextUI; // Nhận TextMeshPro cảnh báo ở giữa màn hình.
    public string warningMessage = "Warning! Zombie is more dangerous at night!"; // Nội dung cảnh báo được hiển thị.
    [Range(0, 23)] public int nightStartHour = 20; // Đặt giờ bắt đầu ban đêm và phát cảnh báo.
    [Range(0, 23)] public int dayStartHour = 7; // Đặt giờ ban ngày bắt đầu và kết thúc cảnh báo ban đêm theo yêu cầu 07:00.
    [Min(1)] public int warningBlinkCount = 5; // Đặt số lần cảnh báo bật sáng tại mỗi mốc giờ.
    [Min(0.01f)] public float warningBlinkInterval = 0.25f; // Đặt thời gian bật hoặc tắt giữa hai lần nhấp nháy.
    public Color warningColor = Color.red; // Đặt màu chữ cảnh báo.

    [Header("Lighting Schedule")] // Tạo tiêu đề nhóm cài đặt lịch chuyển sáng tối.
    [Range(0, 23)] public int sunsetStartHour = 17; // Đặt giờ bắt đầu hoàng hôn để ban ngày không tối sớm.
    [Range(0, 23)] public int dawnStartHour = 4; // Đặt giờ bắt đầu chuyển sáng trước khi trời sáng hẳn.

    [Header("Day Settings")] // Tạo tiêu đề nhóm cài đặt màu và độ sáng ban ngày.
    public Color dayZenith = new Color(0.53f, 0.81f, 0.98f); // Đặt màu vùng đỉnh bầu trời ban ngày.
    public Color dayHorizon = new Color(0.73f, 0.89f, 1f); // Đặt màu vùng chân trời ban ngày.
    public float dayIntensity = 1f; // Đặt cường độ Directional Light vào ban ngày.

    [Header("Night Settings")] // Tạo tiêu đề nhóm cài đặt màu và độ sáng ban đêm.
    public Color nightZenith = new Color(0.01f, 0.01f, 0.07f); // Đặt màu vùng đỉnh bầu trời ban đêm.
    public Color nightHorizon = new Color(0.01f, 0.01f, 0.05f); // Đặt màu vùng chân trời ban đêm.
    public float nightIntensity = 0.05f; // Đặt cường độ Directional Light vào ban đêm.

    private float timer; // Lưu số giây đã chạy trong chu kỳ ngày hiện tại.
    private int dayCount = 1; // Lưu số thứ tự ngày trong game.
    private int lastProcessedGameHour; // Lưu giờ nguyên gần nhất đã kiểm tra cảnh báo.
    private Coroutine warningCoroutine; // Lưu coroutine cảnh báo để có thể dừng hiệu ứng cũ.

    private void Awake() // Chạy một lần khi GameObject được khởi tạo.
    { // Bắt đầu kiểm tra và đăng ký Singleton.
        if (Instance != null && Instance != this) // Kiểm tra đã có một bộ điều khiển khác tồn tại hay chưa.
        { // Bắt đầu nhánh xử lý đối tượng bị trùng.
            Destroy(gameObject); // Hủy bộ điều khiển trùng để tránh hai đồng hồ chạy cùng lúc.
            return; // Dừng Awake trên đối tượng sắp bị hủy.
        } // Kết thúc nhánh xử lý đối tượng bị trùng.

        Instance = this; // Đăng ký đối tượng này để Sleep System có thể truy cập.
    } // Kết thúc Awake.

    private void Start() // Chạy sau Awake để khởi tạo giao diện ban đêm.
    { // Bắt đầu kiểm tra cài đặt và khởi tạo giao diện.
        if (cycleDuration <= 0f) // Kiểm tra thời lượng chu kỳ có hợp lệ hay không.
        { // Bắt đầu nhánh xử lý thời lượng không hợp lệ.
            Debug.LogError("DayAndNightCycle: cycleDuration phải lớn hơn 0.", this); // Báo lỗi rõ ràng trong Console.
            enabled = false; // Tắt script để tránh phép chia cho 0.
            return; // Dừng khởi tạo vì cài đặt không hợp lệ.
        } // Kết thúc nhánh kiểm tra thời lượng.

        if (warningTextUI != null) // Kiểm tra đã gán TextMeshPro cảnh báo trong Inspector hay chưa.
        { // Bắt đầu khởi tạo chữ cảnh báo.
            warningTextUI.text = warningMessage; // Gán nội dung cảnh báo ban đầu.
            warningTextUI.color = warningColor; // Gán màu cảnh báo ban đầu.
            warningTextUI.gameObject.SetActive(false); // Ẩn cả GameObject vì cảnh báo trong Scene có thể đang bị tắt sẵn.
        } // Kết thúc khởi tạo chữ cảnh báo.
        else // Xử lý trường hợp chưa gán đối tượng cảnh báo.
        { // Bắt đầu nhánh thiếu đối tượng cảnh báo.
            Debug.LogWarning("DayAndNightCycle: chưa gán Warning Text UI trong Inspector.", this); // Báo để người dùng biết cần gán TextMeshPro.
        } // Kết thúc nhánh kiểm tra đối tượng cảnh báo.

        float currentGameHour = GetAbsoluteGameHours(); // Lấy giờ tuyệt đối để không phát cảnh báo lặp khi bắt đầu game.
        lastProcessedGameHour = Mathf.FloorToInt(currentGameHour); // Ghi nhận giờ nguyên hiện tại làm mốc kiểm tra ban đầu.
        if (Mathf.Approximately(currentGameHour, lastProcessedGameHour)) // Kiểm tra game có bắt đầu đúng tại một mốc giờ hay không.
        { // Bắt đầu xử lý trường hợp game mở đúng mốc giờ.
            TryShowWarningAtHour(lastProcessedGameHour); // Hiển thị cảnh báo nếu mốc giờ bắt đầu thuộc ban đêm.
        } // Kết thúc kiểm tra mốc giờ bắt đầu.
    } // Kết thúc Start.

    private void Update() // Chạy mỗi khung hình để cập nhật thời gian và hình ảnh.
    { // Bắt đầu cập nhật chu kỳ.
        if (cycleDuration <= 0f) // Kiểm tra thời lượng chu kỳ có bị thay đổi thành giá trị không hợp lệ không.
        { // Bắt đầu nhánh xử lý thời lượng không hợp lệ.
            Debug.LogError("DayAndNightCycle: cycleDuration phải lớn hơn 0.", this); // Báo lỗi cấu hình trong Console.
            enabled = false; // Tắt script để tránh phép chia cho 0.
            return; // Dừng cập nhật khung hình hiện tại.
        } // Kết thúc nhánh kiểm tra thời lượng.

        timer += Time.deltaTime; // Cộng thời gian thực đã trôi qua vào đồng hồ trong game.
        if (timer >= cycleDuration) // Kiểm tra đã hoàn thành một hay nhiều chu kỳ ngày chưa.
        { // Bắt đầu xử lý chu kỳ đã hoàn thành.
            int elapsedDays = Mathf.FloorToInt(timer / cycleDuration); // Tính số ngày đã trôi qua kể cả khi vượt nhiều chu kỳ.
            timer %= cycleDuration; // Giữ lại phần thời gian dư thuộc ngày hiện tại.
            dayCount += elapsedDays; // Cộng số ngày đã trôi qua vào bộ đếm ngày.
        } // Kết thúc xử lý ngày mới.

        float hourOfDay = GetHourOfDay(); // Lấy giờ hiện tại trong khoảng từ 0 đến dưới 24.
        float nightBlend = GetNightBlend(hourOfDay); // Tính mức chuyển đổi giữa ánh sáng ban ngày và ban đêm.
        if (sun != null) // Kiểm tra Directional Light đã được gán hay chưa.
        { // Bắt đầu cập nhật Directional Light.
            sun.transform.rotation = Quaternion.Euler(GetSunAngle(hourOfDay), 0f, 0f); // Xoay Mặt Trời theo lịch bình minh và hoàng hôn.
            sun.intensity = Mathf.Lerp(dayIntensity, nightIntensity, nightBlend); // Giảm độ sáng theo mức chuyển tối đã tính.
        } // Kết thúc cập nhật Directional Light.

        if (skyboxMaterial != null) // Kiểm tra Material bầu trời đã được gán hay chưa.
        { // Bắt đầu cập nhật bầu trời.
            skyboxMaterial.SetColor("_ZenithColor", Color.Lerp(dayZenith, nightZenith, nightBlend)); // Chuyển màu vùng đỉnh trời theo chu kỳ.
            skyboxMaterial.SetColor("_HorizonColor", Color.Lerp(dayHorizon, nightHorizon, nightBlend)); // Chuyển màu vùng chân trời theo chu kỳ.
            skyboxMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.5f, 1f, nightBlend)); // Tăng độ dày khí quyển khi trời tối.
            skyboxMaterial.SetFloat("_EnableStars", nightBlend); // Làm sao xuất hiện dần khi trời tối.
        } // Kết thúc cập nhật bầu trời.

        UpdateTimeUI(); // Cập nhật dòng ngày giờ và màu đồng hồ.
        UpdateNightWarning(); // Kiểm tra đã đến mốc giờ cần phát cảnh báo chưa.
    } // Kết thúc Update.

    private void UpdateTimeUI() // Tạo nội dung đồng hồ theo định dạng ngày và giờ 24 tiếng.
    { // Bắt đầu cập nhật đồng hồ.
        if (timeTextUI == null) // Kiểm tra đã gán TextMeshPro đồng hồ hay chưa.
        { // Bắt đầu nhánh thiếu đồng hồ.
            return; // Không cập nhật nếu giao diện đồng hồ chưa được gán.
        } // Kết thúc kiểm tra đồng hồ.

        int totalMinutes = Mathf.FloorToInt(GetHourOfDay() * 60f); // Chuyển giờ hiện tại thành tổng số phút trong ngày.
        int hours = (totalMinutes / 60) % 24; // Tách giờ và giới hạn kết quả trong định dạng 24 giờ.
        int minutes = totalMinutes % 60; // Tách phần phút còn lại.
        timeTextUI.text = string.Format("Day {0} - {1}:{2:00}", dayCount, hours, minutes); // Hiển thị ví dụ Day 1 - 6:00.
        timeTextUI.color = IsNightTime(hours) ? nightClockColor : normalClockColor; // Đổi màu đồng hồ theo khung giờ nguy hiểm.
    } // Kết thúc cập nhật đồng hồ.

    private void UpdateNightWarning() // Phát cảnh báo một lần ở mỗi mốc giờ ban đêm.
    { // Bắt đầu kiểm tra cảnh báo.
        int currentGameHour = Mathf.FloorToInt(GetAbsoluteGameHours()); // Lấy giờ tuyệt đối nguyên hiện tại.
        if (currentGameHour <= lastProcessedGameHour) // Kiểm tra có mốc giờ mới kể từ lần kiểm tra trước không.
        { // Bắt đầu nhánh chưa sang giờ mới.
            return; // Không cần xử lý lại cùng một giờ.
        } // Kết thúc kiểm tra giờ mới.

        for (int hour = lastProcessedGameHour + 1; hour <= currentGameHour; hour++) // Duyệt mọi mốc giờ đã đi qua trong khung hình này.
        { // Bắt đầu kiểm tra từng mốc giờ.
            TryShowWarningAtHour(hour); // Phát cảnh báo nếu mốc giờ đó nằm trong ban đêm.
        } // Kết thúc vòng lặp kiểm tra giờ.

        lastProcessedGameHour = currentGameHour; // Lưu mốc giờ mới nhất đã xử lý.
    } // Kết thúc kiểm tra cảnh báo.

    private void TryShowWarningAtHour(int absoluteGameHour) // Kiểm tra một mốc giờ tuyệt đối có phải giờ ban đêm không.
    { // Bắt đầu kiểm tra mốc giờ.
        int hourOfDay = (absoluteGameHour % 24 + 24) % 24; // Đổi giờ tuyệt đối thành giờ trong ngày, kể cả khi số giờ âm.
        if (IsNightTime(hourOfDay)) // Kiểm tra mốc giờ có nằm trong khoảng ban đêm hay không.
        { // Bắt đầu nhánh giờ ban đêm.
            ShowNightWarning(); // Bắt đầu hiệu ứng nhấp nháy cảnh báo.
        } // Kết thúc kiểm tra giờ ban đêm.
    } // Kết thúc kiểm tra mốc giờ.

    private void ShowNightWarning() // Bật giao diện cảnh báo và khởi chạy hiệu ứng nhấp nháy.
    { // Bắt đầu bật cảnh báo.
        if (warningTextUI == null) // Kiểm tra đối tượng cảnh báo có được gán trong Inspector không.
        { // Bắt đầu nhánh thiếu giao diện.
            return; // Không thể hiển thị nếu không có TextMeshPro.
        } // Kết thúc kiểm tra giao diện.

        if (warningCoroutine != null) // Kiểm tra hiệu ứng cảnh báo trước đó còn chạy không.
        { // Bắt đầu dừng hiệu ứng cũ.
            StopCoroutine(warningCoroutine); // Dừng hiệu ứng cũ để tránh hai hiệu ứng cùng điều khiển chữ.
        } // Kết thúc kiểm tra coroutine cũ.

        warningTextUI.gameObject.SetActive(true); // Bật GameObject vì trong Scene nó có thể đang inactive.
        warningCoroutine = StartCoroutine(BlinkNightWarning()); // Bắt đầu nhấp nháy chữ trên màn hình.
    } // Kết thúc bật cảnh báo.

    private IEnumerator BlinkNightWarning() // Hiển thị chữ đúng số lần nhấp nháy đã cấu hình.
    { // Bắt đầu coroutine nhấp nháy.
        warningTextUI.text = warningMessage; // Cập nhật nội dung cảnh báo trước khi hiện.
        warningTextUI.color = warningColor; // Đảm bảo chữ cảnh báo dùng đúng màu.
        warningTextUI.enabled = true; // Bật thành phần chữ để bắt đầu lần nhấp nháy đầu tiên.

        for (int blink = 0; blink < warningBlinkCount; blink++) // Lặp đúng số lần nhấp nháy được đặt trong Inspector.
        { // Bắt đầu một lần nhấp nháy.
            warningTextUI.enabled = true; // Hiện chữ cảnh báo.
            yield return new WaitForSeconds(warningBlinkInterval); // Chờ khoảng thời gian chữ được nhìn thấy.
            warningTextUI.enabled = false; // Ẩn chữ để tạo hiệu ứng nhấp nháy.
            yield return new WaitForSeconds(warningBlinkInterval); // Chờ khoảng thời gian chữ bị ẩn.
        } // Kết thúc vòng lặp nhấp nháy.

        warningTextUI.gameObject.SetActive(false); // Tắt GameObject sau khi hoàn tất toàn bộ lần nhấp nháy.
        warningCoroutine = null; // Xóa tham chiếu vì coroutine đã kết thúc.
    } // Kết thúc coroutine nhấp nháy.

    private float GetAbsoluteGameHours() // Tính tổng số giờ đã trôi qua từ khi bắt đầu ngày thứ nhất.
    { // Bắt đầu tính giờ tuyệt đối.
        return (dayCount - 1) * 24f + (timer / cycleDuration) * 24f + startHour; // Kết hợp ngày, tiến độ chu kỳ và giờ bắt đầu.
    } // Kết thúc tính giờ tuyệt đối.

    private float GetHourOfDay() // Lấy giờ hiện tại trong một ngày 24 giờ.
    { // Bắt đầu lấy giờ trong ngày.
        return Mathf.Repeat(GetAbsoluteGameHours(), 24f); // Giữ kết quả trong khoảng từ 0 đến dưới 24.
    } // Kết thúc lấy giờ trong ngày.

    private bool IsNightTime(int hour) // Xác định một giờ nguyên có thuộc ban đêm hay không.
    { // Bắt đầu kiểm tra ban đêm.
        return nightStartHour < dayStartHour // Kiểm tra khoảng ban đêm có đi qua nửa đêm hay không.
            ? hour >= nightStartHour && hour < dayStartHour // Dùng điều kiện này nếu ban đêm nằm trọn trong cùng một ngày.
            : hour >= nightStartHour || hour < dayStartHour; // Dùng điều kiện này nếu ban đêm đi qua nửa đêm.
    } // Kết thúc kiểm tra ban đêm.

    private float GetNightBlend(float hour) // Tính mức tối, trong đó 0 là ban ngày và 1 là ban đêm.
    { // Bắt đầu tính chuyển sắc ngày đêm.
        float duskEndHour = nightStartHour; // Lấy giờ bắt đầu ban đêm làm thời điểm hoàng hôn kết thúc.
        float dawnEndHour = dayStartHour; // Lấy giờ bắt đầu ban ngày làm thời điểm bình minh kết thúc.
        float normalizedHour = Mathf.Repeat(hour, 24f); // Chuẩn hóa giờ để nằm trong khoảng 0 đến dưới 24.
        float normalizedDawnStart = Mathf.Repeat(dawnStartHour, 24f); // Chuẩn hóa giờ bắt đầu bình minh.

        if (normalizedHour >= sunsetStartHour && normalizedHour < duskEndHour) // Kiểm tra đang trong khoảng hoàng hôn.
        { // Bắt đầu chuyển từ màu ban ngày sang ban đêm.
            return Mathf.InverseLerp(sunsetStartHour, duskEndHour, normalizedHour); // Tăng dần độ tối từ 17:00 đến 20:00.
        } // Kết thúc xử lý hoàng hôn.

        if (normalizedHour >= nightStartHour || normalizedHour < normalizedDawnStart) // Kiểm tra đang trong phần đêm sâu.
        { // Bắt đầu giữ trạng thái ban đêm.
            return 1f; // Giữ bầu trời tối hoàn toàn trong khoảng này.
        } // Kết thúc xử lý đêm sâu.

        if (normalizedHour >= normalizedDawnStart && normalizedHour < dawnEndHour) // Kiểm tra đang trong khoảng bình minh.
        { // Bắt đầu chuyển từ màu ban đêm sang ban ngày.
            return 1f - Mathf.InverseLerp(normalizedDawnStart, dawnEndHour, normalizedHour); // Giảm dần độ tối đến giờ bắt đầu ngày.
        } // Kết thúc xử lý bình minh.

        return 0f; // Giữ trạng thái ban ngày trước khi hoàng hôn bắt đầu.
    } // Kết thúc tính chuyển sắc ngày đêm.

    private float GetSunAngle(float hour) // Tính góc xoay Mặt Trời để bình minh lúc 06:00 và hoàng hôn lúc 17:00.
    { // Bắt đầu tính góc Mặt Trời.
        float normalizedHour = Mathf.Repeat(hour, 24f); // Chuẩn hóa giờ trong khoảng 0 đến dưới 24.
        float sunriseHour = dayStartHour; // Dùng giờ bắt đầu ngày làm thời điểm bình minh.
        float sunsetHour = sunsetStartHour; // Dùng giờ hoàng hôn đã cấu hình trong Inspector.
        float daylightDuration = Mathf.Max(0.01f, sunsetHour - sunriseHour); // Tính độ dài ban ngày và tránh chia cho 0.
        float nightDuration = Mathf.Max(0.01f, 24f - daylightDuration); // Tính độ dài ban đêm và tránh chia cho 0.

        if (normalizedHour >= sunriseHour && normalizedHour < sunsetHour) // Kiểm tra giờ hiện tại nằm giữa bình minh và hoàng hôn.
        { // Bắt đầu tính góc vào ban ngày.
            float dayProgress = (normalizedHour - sunriseHour) / daylightDuration; // Tính tiến độ từ bình minh đến hoàng hôn.
            return Mathf.Lerp(0f, 180f, dayProgress); // Đưa Mặt Trời từ chân trời lúc bình minh qua đỉnh trời rồi về chân trời lúc hoàng hôn.
        } // Kết thúc tính góc ban ngày.

        float hoursAfterSunset = normalizedHour >= sunsetHour // Kiểm tra giờ hiện tại nằm sau hoàng hôn cùng ngày.
            ? normalizedHour - sunsetHour // Tính số giờ từ hoàng hôn đến hiện tại.
            : 24f - sunsetHour + normalizedHour; // Tính số giờ qua nửa đêm từ hoàng hôn đến hiện tại.
        float nightProgress = hoursAfterSunset / nightDuration; // Tính tiến độ di chuyển của Mặt Trời trong ban đêm.
        return Mathf.Lerp(-90f, -270f, nightProgress); // Đưa Mặt Trời qua phía dưới đường chân trời đến bình minh kế tiếp.
    } // Kết thúc tính góc Mặt Trời.

    // Hàm công khai này cho phép Sleep System bỏ qua thời gian đến buổi sáng kế tiếp.
    public void SleepUntilMorning() // Cho phép hệ thống ngủ gọi hàm này để chuyển thời gian.
    { // Bắt đầu chuyển thời gian đến sáng.
        if (cycleDuration <= 0f) // Kiểm tra thời lượng chu kỳ trước khi tính thời gian ngủ.
        { // Bắt đầu nhánh cấu hình thời lượng không hợp lệ.
            Debug.LogError("DayAndNightCycle: không thể ngủ khi cycleDuration nhỏ hơn hoặc bằng 0.", this); // Báo lỗi trong Console.
            return; // Dừng hàm vì không thể tính thời gian.
        } // Kết thúc kiểm tra thời lượng.

        float hourOfDay = GetHourOfDay(); // Lấy giờ hiện tại trước khi ngủ.
        float hoursUntilMorning = Mathf.Repeat(dayStartHour - hourOfDay, 24f); // Tính số giờ cần bỏ qua để đến sáng.
        if (hoursUntilMorning < 0.0001f) // Kiểm tra đã đúng giờ sáng hay chưa.
        { // Bắt đầu xử lý trường hợp đã là buổi sáng.
            hoursUntilMorning = 24f; // Chuyển đến buổi sáng của ngày tiếp theo.
        } // Kết thúc xử lý giờ sáng.

        timer += (hoursUntilMorning / 24f) * cycleDuration; // Chuyển số giờ cần ngủ thành số giây thực trong chu kỳ.
        int elapsedDays = Mathf.FloorToInt(timer / cycleDuration); // Tính số chu kỳ ngày đã vượt qua trong lúc ngủ.
        if (elapsedDays > 0) // Kiểm tra có chuyển sang ngày mới hay không.
        { // Bắt đầu cập nhật ngày mới.
            timer %= cycleDuration; // Giữ lại phần thời gian còn dư trong ngày mới.
            dayCount += elapsedDays; // Cập nhật bộ đếm ngày.
        } // Kết thúc cập nhật ngày mới.

        lastProcessedGameHour = Mathf.FloorToInt(GetAbsoluteGameHours()); // Bỏ qua các mốc cảnh báo trong lúc nhân vật ngủ.
        if (warningCoroutine != null) // Kiểm tra cảnh báo cũ có còn đang chạy không.
        { // Bắt đầu dừng cảnh báo cũ.
            StopCoroutine(warningCoroutine); // Dừng hiệu ứng nhấp nháy đang chạy.
            warningCoroutine = null; // Xóa tham chiếu coroutine đã dừng.
        } // Kết thúc xử lý cảnh báo cũ.

        if (warningTextUI != null) // Kiểm tra giao diện cảnh báo đã được gán hay chưa.
        { // Bắt đầu ẩn cảnh báo sau khi ngủ.
            warningTextUI.gameObject.SetActive(false); // Đảm bảo cảnh báo không còn hiện sau khi ngủ dậy.
        } // Kết thúc ẩn cảnh báo.
    } // Kết thúc hàm ngủ đến sáng.

    private void OnDestroy() // Chạy khi đối tượng điều khiển bị hủy.
    { // Bắt đầu dọn Singleton.
        if (Instance == this) // Chỉ xóa Singleton nếu đây là đối tượng đang được đăng ký.
        { // Bắt đầu xóa tham chiếu Singleton.
            Instance = null; // Tránh để hệ thống khác gọi nhầm đối tượng đã bị hủy.
        } // Kết thúc kiểm tra Singleton.
    } // Kết thúc OnDestroy.
} // Kết thúc lớp DayAndNightCycle.
