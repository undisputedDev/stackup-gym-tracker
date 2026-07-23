using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AView = Android.Views.View;

namespace SimpleGymDiary.App.Controls;

/// <summary>
/// Android-only "drag armed" feedback for reorderable CollectionViews. TouchBehavior
/// never sees touches inside a RecyclerView with an ItemTouchHelper, so this observes
/// the raw touch stream via a non-intercepting OnItemTouchListener: when a press is
/// held still to the long-press threshold (the moment ItemTouchHelper arms the drag),
/// the pressed row pulses up briefly with a haptic tick. The pulse is self-resetting,
/// so recycled rows can never get stuck scaled. (iOS would need an equivalent hook;
/// Windows starts drags immediately on mouse move and needs no affordance.)
/// </summary>
public static class DragLift
{
    public static void Enable(CollectionView collectionView)
    {
        collectionView.HandlerChanged += (_, _) =>
        {
            if (collectionView.Handler?.PlatformView is RecyclerView recycler)
                recycler.AddOnItemTouchListener(new LiftListener());
        };
    }

    private sealed class LiftListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
    {
        private readonly Handler _timer = new(Looper.MainLooper!);
        private Java.Lang.Runnable? _pending;
        private float _downX;
        private float _downY;

        public bool OnInterceptTouchEvent(RecyclerView rv, MotionEvent e)
        {
            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                    _downX = e.GetX();
                    _downY = e.GetY();
                    var pressed = rv.FindChildViewUnder(_downX, _downY);
                    if (pressed is not null)
                    {
                        Cancel();
                        _pending = new Java.Lang.Runnable(() => Pulse(pressed));
                        // Just before ItemTouchHelper's ~400 ms long-press takes over.
                        _timer.PostDelayed(_pending, 330);
                    }
                    break;

                case MotionEventActions.Move:
                    var slop = ViewConfiguration.Get(rv.Context!)!.ScaledTouchSlop;
                    if (Math.Abs(e.GetX() - _downX) > slop || Math.Abs(e.GetY() - _downY) > slop)
                        Cancel(); // scrolling, not holding
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    Cancel();
                    break;
            }

            return false; // observe only, never claim the touch
        }

        public void OnTouchEvent(RecyclerView rv, MotionEvent e)
        {
        }

        public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept)
        {
        }

        private void Cancel()
        {
            if (_pending is not null)
            {
                _timer.RemoveCallbacks(_pending);
                _pending = null;
            }
        }

        private static void Pulse(AView view)
        {
            view.PerformHapticFeedback(FeedbackConstants.LongPress);
            view.Animate()!
                .ScaleX(1.04f).ScaleY(1.04f)
                .SetDuration(110)
                .WithEndAction(new Java.Lang.Runnable(() =>
                    view.Animate()!
                        .ScaleX(1f).ScaleY(1f)
                        .SetStartDelay(180)
                        .SetDuration(220)
                        .Start()))
                .Start();
        }
    }
}
