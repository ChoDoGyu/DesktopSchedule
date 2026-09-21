using System.Windows;
using System.Windows.Controls;

namespace DesktopSchedule.Controls;

/// <summary>
/// 일정 Drag 중 마우스를 따라다니는 공통 미리보기 카드입니다.
/// 주간과 월간 화면의 Canvas 위에 배치하여 재사용할 수 있습니다.
/// </summary>
public class ScheduleDragPreview : Border
{
    private readonly TextBlock _textBlock;

    public ScheduleDragPreview()
    {
        _textBlock = new TextBlock
        {
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        };

        Background = SystemColors.ControlBrush;
        BorderBrush = SystemColors.ControlDarkBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(3);
        Padding = new Thickness(5, 2, 5, 2);
        Opacity = 0.85;
        IsHitTestVisible = false;
        Visibility = Visibility.Collapsed;
        Child = _textBlock;
    }

    /// <summary>
    /// Drag할 일정의 내용을 설정하고 미리보기 카드를 표시합니다.
    /// </summary>
    public void Show(string text, double width, double height)
    {
        _textBlock.Text = text;

        Width = Math.Max(100, width);
        Height = Math.Max(28, height);

        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 마우스 위치와 사용자가 카드를 처음 잡은 위치를 이용해
    /// Canvas 위의 미리보기 위치를 갱신합니다.
    /// </summary>
    public void Move(Point cursorPosition, Point pointerOffset)
    {
        Canvas.SetLeft(this, cursorPosition.X - pointerOffset.X);
        Canvas.SetTop(this, cursorPosition.Y - pointerOffset.Y);
    }

    /// <summary>
    /// Drag 미리보기를 즉시 숨깁니다.
    /// </summary>
    public void Hide()
    {
        Visibility = Visibility.Collapsed;
    }
}