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

export interface PagedResult<T> {
  items: T[];
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
