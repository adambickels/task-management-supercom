export interface Tag {
  id: number;
  name: string;
}

export interface TaskItem {
  id: number;
  title: string;
  description: string;
  dueDate: string;
  priority: number;
  fullName: string;
  telephone: string;
  email: string;
  tagIds: number[];
  tags: Tag[];
  createdAt: string;
  updatedAt?: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  dueDate: string;
  priority: number;
  fullName: string;
  telephone: string;
  email: string;
  tagIds: number[];
}
