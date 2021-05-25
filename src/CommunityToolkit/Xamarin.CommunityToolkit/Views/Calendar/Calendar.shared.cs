using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.CommunityToolkit.Extensions;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Xamarin.CommunityToolkit.UI.Views
{
	public class Calendar : ContentView
	{
		readonly List<CalendarDay> days = new();
		readonly Grid gridDays;
		readonly Grid gridWeekDayHeaders;

		/// <summary>
		/// Gets days that are currently visible.
		/// </summary>
		public IReadOnlyCollection<CalendarDay> Days => days.Where(x => x.IsVisible).ToList();

		/// <summary>
		/// Event that is triggered when the <see cref="CalendarDay" /> is tapped.
		/// </summary>
		public event EventHandler<CalendarDayTappedEventArgs>? DayTapped;

		/// <summary>
		/// Event that is triggered when the visible <see cref="CalendarDay" /> is updated.
		/// </summary>
		public event EventHandler<CalendarDayUpdatedEventArgs>? DayUpdated;

		/// <summary>
		/// Backing BindableProperty for the <see cref="ShowDaysFromOtherMonths"/> property.
		/// </summary>
		public static readonly BindableProperty ShowDaysFromOtherMonthsProperty =
			BindableProperty.Create(nameof(ShowDaysFromOtherMonths), typeof(bool), typeof(Calendar), true, propertyChanged: OnShowDaysFromOtherMonthsPropertyChanged);

		/// <summary>
		/// Determines if days from other months should be visible.
		/// </summary>
		public bool ShowDaysFromOtherMonths
		{
			get => (bool)GetValue(ShowDaysFromOtherMonthsProperty);
			set => SetValue(ShowDaysFromOtherMonthsProperty, value);
		}

		static void OnShowDaysFromOtherMonthsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var calendar = (Calendar)bindable;
			calendar.UpdateCalendarDays();
		}

		/// <summary>
		/// Backing BindableProperty for the <see cref="ShowWeekends"/> property.
		/// </summary>
		public static readonly BindableProperty ShowWeekendsProperty =
			BindableProperty.Create(nameof(ShowWeekends), typeof(bool), typeof(Calendar), true, propertyChanged: OnShowWeekendsChanged);

		/// <summary>
		/// Determines if weekends should be visible.
		/// </summary>
		public bool ShowWeekends
		{
			get => (bool)GetValue(ShowWeekendsProperty);
			set => SetValue(ShowWeekendsProperty, value);
		}

		static void OnShowWeekendsChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var calendar = (Calendar)bindable;

			if (!calendar.ShowWeekends && (calendar.FirstDayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
			{
				calendar.FirstDayOfWeek = DayOfWeek.Monday;
			}
			else
			{
				calendar.UpdateWeekDayHeaders();
				calendar.UpdateCalendarDays();
			}
		}

		/// <summary>
		/// Backing BindableProperty for the <see cref="FirstDayOfWeek"/> property.
		/// </summary>
		public static readonly BindableProperty FirstDayOfWeekProperty =
			BindableProperty.Create(
				propertyName: nameof(WeekDayHeaderControlTemplateProperty),
				returnType: typeof(DayOfWeek),
				declaringType: typeof(Calendar),
				defaultValue: DayOfWeek.Monday,
				propertyChanged: OnFirstDayOfWeekChanged);

		/// <summary>
		/// Gets or sets first day of week.
		/// </summary>
		public DayOfWeek FirstDayOfWeek
		{
			get => (DayOfWeek)GetValue(FirstDayOfWeekProperty);
			set => SetValue(FirstDayOfWeekProperty, value);
		}

		static void OnFirstDayOfWeekChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var calendar = (Calendar)bindable;
			calendar.UpdateWeekDayHeaders();
			calendar.UpdateCalendarDays();
		}

		/// <summary>
		/// Backing BindableProperty for the <see cref="WeekDayHeaderControlTemplate"/> property.
		/// </summary>
		public static readonly BindableProperty WeekDayHeaderControlTemplateProperty =
			BindableProperty.Create(nameof(WeekDayHeaderControlTemplateProperty), typeof(ControlTemplate),
				typeof(Calendar), propertyChanged: OnWeekDayHeaderControlTemplateChanged);

		/// <summary>
		/// Gets or sets <see cref="ControlTemplate"/> for week day header at the top of <see cref="Calendar"/>.
		/// </summary>
		public ControlTemplate? WeekDayHeaderControlTemplate
		{
			get => (ControlTemplate?)GetValue(WeekDayHeaderControlTemplateProperty);
			set => SetValue(WeekDayHeaderControlTemplateProperty, value);
		}

		static void OnWeekDayHeaderControlTemplateChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var calendar = (Calendar)bindable;
			calendar.UpdateWeekDayHeaders();
		}

		/// <summary>
		/// Backing BindableProperty for the <see cref="DayControlTemplate"/> property.
		/// </summary>
		public static readonly BindableProperty DayControlTemplateProperty =
			BindableProperty.Create(nameof(DayControlTemplate), typeof(ControlTemplate), typeof(Calendar),
				propertyChanged: OnDayControlTemplateChanged);

		/// <summary>
		/// Gets or sets <see cref="ControlTemplate"/> for <see cref="CalendarDay"/>.
		/// </summary>
		public ControlTemplate? DayControlTemplate
		{
			get => (ControlTemplate?)GetValue(DayControlTemplateProperty);
			set => SetValue(DayControlTemplateProperty, value);
		}

		static void OnDayControlTemplateChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var calendar = (Calendar)bindable;
			calendar.UpdateCalendarDays();
		}

		/// <summary>
		/// Backing BindableProperty for the <see cref="SelectedDays"/> property.
		/// </summary>
		public static readonly BindableProperty SelectedDaysProperty =
			BindableProperty.Create(nameof(SelectedDays), typeof(IReadOnlyCollection<DateTime>), typeof(Calendar), Enumerable.Empty<DateTime>().ToList(), propertyChanged: OnSelectedDaysChanged);

		/// <summary>
		/// Gets or sets selected days.
		/// </summary>
		public IReadOnlyCollection<DateTime> SelectedDays
		{
			get => (IReadOnlyCollection<DateTime>)GetValue(SelectedDaysProperty);
			set => SetValue(SelectedDaysProperty, value);
		}

		static void OnSelectedDaysChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var daysToSelect = (IList<DateTime>)newValue;
			var calendar = (Calendar)bindable;

			if (calendar.SelectionMode is CalendarSelectionMode.SingleSelect
				&& daysToSelect != null
				&& daysToSelect.Count > 1)
			{
				throw new InvalidOperationException(
					$"Cannot select more than one day when working in {nameof(CalendarSelectionMode.SingleSelect)} mode");
			}

			calendar.SelectDays(daysToSelect);
		}

		/// <summary>
		/// Backing BindableProperty for the <see cref="Date"/> property.
		/// </summary>
		public static readonly BindableProperty DateProperty =
			BindableProperty.Create(nameof(Date), typeof(DateTime), typeof(Calendar), default(DateTime), propertyChanged: OnDateChanged);

		/// <summary>
		/// Gets or sets the date.
		/// </summary>
		public DateTime Date
		{
			get => (DateTime)GetValue(DateProperty);
			set => SetValue(DateProperty, value);
		}

		static void OnDateChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var calendar = (Calendar)bindable;
			calendar.UpdateCalendarDays();
		}

		/// <summary>
		/// Backing BindableProperty for the <see cref="SelectionMode"/> property.
		/// </summary>
		public static readonly BindableProperty SelectionModeProperty =
			BindableProperty.Create(nameof(SelectionMode), typeof(CalendarSelectionMode), typeof(Calendar), CalendarSelectionMode.SingleSelect, propertyChanged: OnModeChanged);

		/// <summary>
		/// Gets or sets the mode, which determines how <see cref="Calendar"/> handles days selection.
		/// </summary>
		public CalendarSelectionMode SelectionMode
		{
			get => (CalendarSelectionMode)GetValue(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		static void OnModeChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var calendar = (Calendar)bindable;
			calendar.SelectedDays = Enumerable.Empty<DateTime>().ToList();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Calendar"/> class.
		/// </summary>
		public Calendar()
		{
			var layoutRoot = new Grid
			{
				Padding = 0,
				ColumnSpacing = 0,
				RowSpacing = 0
			};

			layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			var gridWeekDayHeaders = new Grid();
			gridWeekDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridWeekDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridWeekDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridWeekDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridWeekDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridWeekDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridWeekDayHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

			Grid.SetRow(gridWeekDayHeaders, 0);
			layoutRoot.Children.Add(gridWeekDayHeaders);
			this.gridWeekDayHeaders = gridWeekDayHeaders;

			var gridDays = new Grid
			{
				ColumnSpacing = 0
			};

			gridDays.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridDays.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridDays.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridDays.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridDays.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridDays.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
			gridDays.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

			Grid.SetRow(gridDays, 1);
			layoutRoot.Children.Add(gridDays);
			this.gridDays = gridDays;

			Content = layoutRoot;
		}

		void UpdateCalendarDays()
		{
			UpdateGridForDays();
			UpdateDays();
			SelectDays(SelectedDays);
		}

		void SelectDays(IEnumerable<DateTime>? daysToSelect)
		{
			if (daysToSelect == null)
			{
				return;
			}

			days.ForEach(calendarDay => calendarDay.IsSelected = false);

			foreach (var dayToSelect in daysToSelect)
			{
				var calendarDay = days.FirstOrDefault(x => x.Date == dayToSelect.Date);
				if (calendarDay != null)
				{
					calendarDay.IsSelected = true;
				}
			}
		}

		void UpdateGridForDays()
		{
			if (!ShowWeekends)
			{
				gridDays.ColumnDefinitions[5].Width = new GridLength(0);
				gridDays.ColumnDefinitions[6].Width = new GridLength(0);
			}
			else if (ShowWeekends)
			{
				gridDays.ColumnDefinitions[5].Width = GridLength.Star;
				gridDays.ColumnDefinitions[6].Width = GridLength.Star;
			}

			var weeksInMonth = Date.WeeksInMonth(FirstDayOfWeek);

			if (gridDays.RowDefinitions.Count < weeksInMonth)
			{
				for (var row = gridDays.RowDefinitions.Count; row < weeksInMonth; row++)
				{
					gridDays.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

					var daysToAdd = days.Where(x => Grid.GetRow(x) == row).ToList();

					foreach (var calendarDay in daysToAdd)
					{
						gridDays.Children.Add(calendarDay);
					}
				}
			}
			else if (gridDays.RowDefinitions.Count > weeksInMonth)
			{
				for (var row = gridDays.RowDefinitions.Count; row > weeksInMonth; row--)
				{
					gridDays.RowDefinitions.RemoveAt(gridDays.RowDefinitions.Count - 1);

					var daysToRemove = days.Where(x => Grid.GetRow(x) == row - 1).ToList();

					foreach (var calendarDay in daysToRemove)
					{
						gridDays.Children.Remove(calendarDay);
					}
				}
			}
		}

		void UpdateDays()
		{
			var weeksInMonth = Date.WeeksInMonth(FirstDayOfWeek);
			var daysInMonth = Date.DaysInMonth();

			for (var day = 1; day <= daysInMonth; day++)
			{
				var date = new DateTime(Date.Year, Date.Month, day);
				var weekOfMonth = date.WeekOfMonth(FirstDayOfWeek);

				UpdateDaysFromPreviousOrNextMonths(weekOfMonth, date, weeksInMonth, daysInMonth);

				if (!ShowWeekends && (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
				{
					continue;
				}

				UpdateDay(date, weekOfMonth, isVisible: true);
			}

			if (SelectedDays?.Count > 0)
			{
				SelectDays(SelectedDays);
			}
		}

		void UpdateDaysFromPreviousOrNextMonths(
			int week,
			DateTime date,
			int weeksInMonth,
			int daysInMonth)
		{
			if (week == 1
				&& date.Day == 1
				&& date.DayOfWeek != FirstDayOfWeek)
			{
				var newDate = date;

				do
				{
					newDate = newDate.AddDays(-1);

					if (!ShowWeekends && (newDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
					{
						continue;
					}

					UpdateDay(newDate, week, isVisible: ShowDaysFromOtherMonths);
				} while (newDate.DayOfWeek != FirstDayOfWeek);
			}
			else if (week == weeksInMonth
					 && date.Day == daysInMonth
					 && date.DayOfWeek != FirstDayOfWeek.PreviousOrFirst())
			{
				var newDate = date;
				var lastDayOfWeek = FirstDayOfWeek.PreviousOrFirst();

				do
				{
					newDate = newDate.AddDays(1);

					if (!ShowWeekends && (newDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
					{
						continue;
					}

					UpdateDay(newDate, week, isVisible: ShowDaysFromOtherMonths);
				} while (newDate.DayOfWeek != lastDayOfWeek);
			}
		}

		void UpdateDay(DateTime date, int week, bool isVisible)
		{
			var column = date.DayOfWeek(FirstDayOfWeek, includeWeekends: ShowWeekends) - 1;
			var row = week - 1;

			var calendarDay = days.FirstOrDefault(x => Grid.GetColumn(x) == column && Grid.GetRow(x) == row);

			if (calendarDay == null)
			{
				calendarDay = new CalendarDay
				{
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					VerticalOptions = LayoutOptions.CenterAndExpand,
					ControlTemplate = DayControlTemplate,
					Date = date,
				};

				if (isVisible)
				{
					calendarDay.Opacity = 1;
				}
				else
				{
					calendarDay.Opacity = 0;
				}

				Grid.SetColumn(calendarDay, column);
				Grid.SetRow(calendarDay, row);

				var tapGestureRecognizer = new TapGestureRecognizer();
				tapGestureRecognizer.Tapped += CalendarDay_OnTapped;
				calendarDay.GestureRecognizers.Add(tapGestureRecognizer);

				days.Add(calendarDay);
				gridDays.Children.Add(calendarDay);

				if (isVisible)
				{
					DayUpdated?.Invoke(this, new CalendarDayUpdatedEventArgs(calendarDay));
				}
			}
			else
			{
				calendarDay.Date = date;
				calendarDay.ControlTemplate = DayControlTemplate;

				if (isVisible)
				{
					DayUpdated?.Invoke(this, new CalendarDayUpdatedEventArgs(calendarDay));
				}

				Device.BeginInvokeOnMainThread(() =>
				{
					if (isVisible)
					{
						calendarDay.Opacity = 1;
					}
					else
					{
						calendarDay.Opacity = 0;
					}
				});
			}
		}

		void CalendarDay_OnTapped(object sender, EventArgs e)
		{
			var calendarDay = (CalendarDay)sender;

			if (calendarDay.IsSelectable)
			{
				if (SelectionMode == CalendarSelectionMode.SingleSelect)
				{
					SelectedDays = new[] { calendarDay.Date };
				}
				else if (SelectionMode == CalendarSelectionMode.MultiSelect)
				{
					var newSelectedDays = SelectedDays.ToList();

					if (calendarDay.IsSelected)
					{
						newSelectedDays.Remove(calendarDay.Date);
					}
					else
					{
						newSelectedDays.Add(calendarDay.Date);
					}

					SelectedDays = new ReadOnlyCollection<DateTime>(newSelectedDays);
				}
			}

			DayTapped?.Invoke(this, new CalendarDayTappedEventArgs(calendarDay));
		}

		void UpdateWeekDayHeaders()
		{
			if (WeekDayHeaderControlTemplate == null)
			{
				return;
			}

			gridWeekDayHeaders.Children.Clear();

			if (!ShowWeekends)
			{
				gridWeekDayHeaders.ColumnDefinitions[5].Width = new GridLength(0);
				gridWeekDayHeaders.ColumnDefinitions[6].Width = new GridLength(0);
			}
			else if (ShowWeekends)
			{
				gridWeekDayHeaders.ColumnDefinitions[5].Width = GridLength.Star;
				gridWeekDayHeaders.ColumnDefinitions[6].Width = GridLength.Star;
			}

			void AddOrUpdateWeekDayHeaderControl(DayOfWeek dayOfWeek, int weekDayNumber)
			{
				var column = weekDayNumber - 1;

				if (gridWeekDayHeaders.Children.FirstOrDefault(x => Grid.GetColumn(x) == column) is not CalendarWeekDayHeader calendarWeekDayHeader)
				{
					var weekDayControl = new CalendarWeekDayHeader(dayOfWeek)
					{
						ControlTemplate = WeekDayHeaderControlTemplate,
					};

					Grid.SetColumn(weekDayControl, column);
					gridWeekDayHeaders.Children.Add(weekDayControl);
				}
			}

			var currentDayOfWeek = FirstDayOfWeek;
			var daysInWeek = 7;

			if (!ShowWeekends)
			{
				daysInWeek = 5;
			}

			for (var i = 1; i <= daysInWeek; i++)
			{
				AddOrUpdateWeekDayHeaderControl(currentDayOfWeek, weekDayNumber: i);

				currentDayOfWeek = currentDayOfWeek.NextOrFirst();

				if (!ShowWeekends && (currentDayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
				{
					currentDayOfWeek = DayOfWeek.Monday;
				}
			}
		}
	}
}