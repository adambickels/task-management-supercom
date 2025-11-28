import axios from 'axios';
import type { TaskItem, CreateTaskRequest, Tag } from '../types';

const API_BASE_URL = 'http://localhost:5119/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const taskApi = {
  getAllTasks: async (): Promise<TaskItem[]> => {
    const response = await api.get<TaskItem[]>('/tasks');
    return response.data;
  },

  getTask: async (id: number): Promise<TaskItem> => {
    const response = await api.get<TaskItem>(`/tasks/${id}`);
    return response.data;
  },

  createTask: async (task: CreateTaskRequest): Promise<TaskItem> => {
    const response = await api.post<TaskItem>('/tasks', task);
    return response.data;
  },

  updateTask: async (id: number, task: CreateTaskRequest): Promise<TaskItem> => {
    const response = await api.put<TaskItem>(`/tasks/${id}`, { ...task, id });
    return response.data;
  },

  deleteTask: async (id: number): Promise<void> => {
    await api.delete(`/tasks/${id}`);
  },
};

export const tagApi = {
  getAllTags: async (): Promise<Tag[]> => {
    const response = await api.get<Tag[]>('/tags');
    return response.data;
  },

  getTag: async (id: number): Promise<Tag> => {
    const response = await api.get<Tag>(`/tags/${id}`);
    return response.data;
  },

  createTag: async (tag: Omit<Tag, 'id'>): Promise<Tag> => {
    const response = await api.post<Tag>('/tags', tag);
    return response.data;
  },

  updateTag: async (id: number, tag: Omit<Tag, 'id'>): Promise<Tag> => {
    const response = await api.put<Tag>(`/tags/${id}`, { ...tag, id });
    return response.data;
  },

  deleteTag: async (id: number): Promise<void> => {
    await api.delete(`/tags/${id}`);
  },
};
