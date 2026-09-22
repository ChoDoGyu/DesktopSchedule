using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DesktopSchedule.Controls;

/// <summary>
/// DesktopSchedule에서 공통으로 사용하는 시간 선택 컨트롤입니다.
/// 시와 분을 키보드로 직접 입력하거나 마우스 휠로 1단위씩 조절할 수 있습니다.
/// </summary>
public partial class ScheduleTimePicker : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ScheduleTimePicker),
            new FrameworkPropertyMetadata(
                "00:00",
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextPropertyChanged));

    private string _originalText = "00:00";
    private int _hour;
    private int _minute;
    private bool _cancelRequested;

    /// <summary>
    /// HH:mm 형식으로 표시하고 외부 ViewModel과 주고받는 시간 문자열입니다.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ScheduleTimePicker()
    {
        InitializeComponent();

        DisplayTextBlock.Text = Text;
    }

    /// <summary>
    /// 외부 Binding에서 시간 값이 변경되면 화면에 표시되는 값을 함께 갱신합니다.
    /// </summary>
    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScheduleTimePicker picker || picker.DisplayTextBlock is null)
        {
            return;
        }

        picker.DisplayTextBlock.Text = e.NewValue as string ?? string.Empty;
    }

    /// <summary>
    /// 시간 표시 영역을 클릭하면 현재 값을 시와 분으로 분리하여 팝업을 엽니다.
    /// </summary>
    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        _originalText = Text;
        _cancelRequested = false;

        LoadFieldsFromText();

        TimePopup.IsOpen = true;

        Dispatcher.BeginInvoke(
            () =>
            {
                HourTextBox.Focus();
                HourTextBox.SelectAll();
            },
            DispatcherPriority.Input);
    }

    /// <summary>
    /// 현재 Text 값을 시와 분으로 해석하여 팝업 입력 필드에 표시합니다.
    /// 유효하지 않은 값이면 00:00을 사용합니다.
    /// </summary>
    private void LoadFieldsFromText()
    {
        if (!TimeSpan.TryParse(Text, CultureInfo.InvariantCulture, out var time) ||
            time < TimeSpan.Zero ||
            time >= TimeSpan.FromDays(1))
        {
            _hour = 0;
            _minute = 0;
        }
        else
        {
            _hour = time.Hours;
            _minute = time.Minutes;
        }

        UpdateFieldTexts();
    }

    /// <summary>
    /// 현재 시와 분 값을 두 자리 문자열로 입력 필드에 표시합니다.
    /// </summary>
    private void UpdateFieldTexts()
    {
        HourTextBox.Text = _hour.ToString("D2", CultureInfo.InvariantCulture);
        MinuteTextBox.Text = _minute.ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 현재 시와 분 값을 HH:mm 형식으로 외부 Binding 값에 반영합니다.
    /// </summary>
    private void UpdateBoundText()
    {
        Text = $"{_hour:D2}:{_minute:D2}";
    }

    /// <summary>
    /// 숫자 입력 필드에는 0~9 숫자만 직접 입력할 수 있도록 합니다.
    /// </summary>
    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }

    /// <summary>
    /// 시 또는 분 입력 필드를 클릭하면 기존 값을 전체 선택하여
    /// 새로운 숫자를 바로 입력할 수 있도록 합니다.
    /// </summary>
    private void NumericTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    /// <summary>
    /// 시간 입력 필드에서 포커스가 빠질 때 0~23 범위로 정리하고 두 자리로 표시합니다.
    /// </summary>
    private void HourTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        NormalizeHour();
        UpdateBoundText();
    }

    /// <summary>
    /// 분 입력 필드에서 포커스가 빠질 때 0~59 범위로 정리하고 두 자리로 표시합니다.
    /// </summary>
    private void MinuteTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        NormalizeMinute();
        UpdateBoundText();
    }

    /// <summary>
    /// 시간 숫자 위에서 휠을 올리면 1 감소하고 내리면 1 증가합니다.
    /// 00과 23의 경계를 넘으면 반대쪽 값으로 순환합니다.
    /// </summary>
    private void HourTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        NormalizeHour();

        _hour = ChangeCircularValue(_hour, e.Delta > 0 ? -1 : 1, 23);

        HourTextBox.Text = _hour.ToString("D2", CultureInfo.InvariantCulture);
        UpdateBoundText();

        e.Handled = true;
    }

    /// <summary>
    /// 분 숫자 위에서 휠을 올리면 1 감소하고 내리면 1 증가합니다.
    /// 00과 59의 경계를 넘으면 반대쪽 값으로 순환합니다.
    /// </summary>
    private void MinuteTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        NormalizeMinute();

        _minute = ChangeCircularValue(_minute, e.Delta > 0 ? -1 : 1, 59);

        MinuteTextBox.Text = _minute.ToString("D2", CultureInfo.InvariantCulture);
        UpdateBoundText();

        e.Handled = true;
    }

    /// <summary>
    /// Enter를 누르면 현재 값을 적용하고 닫으며,
    /// Escape를 누르면 팝업을 열기 전 값으로 되돌리고 닫습니다.
    /// </summary>
    private void TimeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitAndClose();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            _cancelRequested = true;
            Text = _originalText;
            TimePopup.IsOpen = false;
            e.Handled = true;
        }
    }

    /// <summary>
    /// 현재 입력값을 정상 범위로 정리한 뒤 적용하고 팝업을 닫습니다.
    /// </summary>
    private void CommitAndClose()
    {
        NormalizeHour();
        NormalizeMinute();
        UpdateFieldTexts();
        UpdateBoundText();

        TimePopup.IsOpen = false;
    }

    /// <summary>
    /// 팝업 밖을 클릭하여 닫힌 경우에도 현재 입력값을 적용합니다.
    /// Escape로 닫은 경우에는 원래 값을 유지합니다.
    /// </summary>
    private void TimePopup_Closed(object? sender, EventArgs e)
    {
        if (_cancelRequested)
        {
            _cancelRequested = false;
            return;
        }

        NormalizeHour();
        NormalizeMinute();
        UpdateFieldTexts();
        UpdateBoundText();
    }

    /// <summary>
    /// 시간 입력값을 0~23 범위로 정리합니다.
    /// 비어 있거나 숫자가 아니면 마지막 정상 값을 다시 사용합니다.
    /// </summary>
    private void NormalizeHour()
    {
        if (!int.TryParse(HourTextBox.Text, out var value))
        {
            HourTextBox.Text = _hour.ToString("D2", CultureInfo.InvariantCulture);
            return;
        }

        _hour = Math.Clamp(value, 0, 23);
        HourTextBox.Text = _hour.ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 분 입력값을 0~59 범위로 정리합니다.
    /// 비어 있거나 숫자가 아니면 마지막 정상 값을 다시 사용합니다.
    /// </summary>
    private void NormalizeMinute()
    {
        if (!int.TryParse(MinuteTextBox.Text, out var value))
        {
            MinuteTextBox.Text = _minute.ToString("D2", CultureInfo.InvariantCulture);
            return;
        }

        _minute = Math.Clamp(value, 0, 59);
        MinuteTextBox.Text = _minute.ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 지정한 범위를 넘어가면 반대쪽 끝으로 순환하면서 값을 변경합니다.
    /// </summary>
    private static int ChangeCircularValue(int value, int change, int maximum)
    {
        var changedValue = value + change;

        if (changedValue < 0)
        {
            return maximum;
        }

        if (changedValue > maximum)
        {
            return 0;
        }

        return changedValue;
    }
}