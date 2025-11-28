import axios from 'axios';
import type { TaskItem, CreateTaskRequest, Tag, PagedResult } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5119/api/v1.0';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add response interceptor for better error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      // Server responded with error
      const message = error.response.data?.detail || error.response.data?.message || error.message;
      throw new Error(message);
    } else if (error.request) {
      // Request made but no response
      throw new Error('Network error. Please check your connection.');
    } else {
      // Something else happened
      throw new Error(error.message || 'An unexpected error occurred');
    }
  }
);

const DEFAULT_PAGE_SIZE = Number(import.meta.env.VITE_DEFAULT_PAGE_SIZE) || 10;

export const taskApi = {
  getAllTasks: async (page: number = 1, pageSize: number = DEFAULT_PAGE_SIZE): Promise<PagedResult<TaskItem>> => {
    const response = await api.get<PagedResult<TaskItem>>('/tasks', {
      params: { page, pageSize }
    });
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
