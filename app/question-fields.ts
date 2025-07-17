export enum QuestionField {
  ContactName = 1,
  ContactEmail = 2,
  ContactPhone = 3,
  TicketDescription = 4,
}

export function getQuestionField(id: number): string {
  switch (id) {
    case QuestionField.ContactName:
      return 'Contact Name';
    case QuestionField.ContactEmail:
      return 'Contact Email';
    case QuestionField.ContactPhone:
      return 'Contact Phone';
    case QuestionField.TicketDescription:
      return 'Ticket Description';
    default:
      return `Unknown (${id})`;
  }
}
