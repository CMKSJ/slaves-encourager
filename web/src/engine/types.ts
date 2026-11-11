// 共享类型，逐行对照 src/SlavesEncourager/Engine.cs / Shared types mirroring Engine.cs.
export interface RewardGoal {
  Name: string;
  Amount: number;
}

export interface GoalProgress {
  Goal: RewardGoal;
  Allocated: number;
  RemainingToReach: number;
  Fraction: number;
}

export interface ExpenseProgress {
  Item: RewardGoal;
  Covered: number;
  Remaining: number;
  RemainingFraction: number;
}

export interface Interval {
  Start: Date;
  End: Date;
}

export interface Snapshot {
  Today: number;
  Month: number;
  Hourly: number;
  Daily: number;
  Rate: number;
  Worked: number;
  Total: number;
  Progress: number;
  MonthDays: number;
  StatusKey: string;
  CountdownKey: string;
  CountdownSeconds: number;
  WorkDate: Date;
  PeriodStart: Date;
  PeriodEnd: Date;
}

// 设置字段保留 PascalCase，与桌面版 JSON 契约一致 / Settings keep PascalCase fields to match the desktop JSON contract.
export interface Settings {
  MonthlySalary: number;
  Start: string;
  End: string;
  LunchStart: string;
  LunchEnd: string;
  LunchEnabled: boolean;
  Weekdays: string;
  RestDates: string;
  WorkDates: string;
  Basis: 'actual' | 'fixed' | string;
  Decimals: number;
  Grouping: boolean;
  Currency: string;
  Pattern: string;
  Animation: string;
  Font: string;
  Theme: string;
  Speed: number;
  GoalName: string;
  GoalAmount: number;
  Goals: RewardGoal[] | null;
  Topmost: boolean;
  Configured: boolean;
  Language: string | null;
  HomeText: Record<string, string> | null;
  ExpensesEnabled: boolean;
  Expenses: RewardGoal[] | null;
  SettlementDay: number | null;
  OpeningAt: string | null;
  OpeningIncome: number | null;
}

// 校验错误携带翻译键，由界面层翻译 / Validation errors carry a translation key for the UI layer.
export class ValidationError extends Error {
  constructor(public key: string) {
    super(key);
  }
}
