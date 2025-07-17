export enum QuestionType {
  ShortText = 1,
  LongText = 2,
  Dropdown = 3,
  Checkbox = 4,
  Hidden = 5,
}

export function getQuestionType(id: number): string {
  switch (id) {
    case QuestionType.ShortText:
      return 'Short Text';
    case QuestionType.LongText:
      return 'Long Text';
    case QuestionType.Dropdown:
      return 'Dropdown';
    case QuestionType.Checkbox:
      return 'Checkbox';
    case QuestionType.Hidden:
      return 'Hidden';
    default:
      return `Unknown (${id})`;
  }
}
