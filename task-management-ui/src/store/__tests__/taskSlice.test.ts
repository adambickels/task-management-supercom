import { describe, it, expect, vi } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';
import taskReducer, {
  loadTasks,
  setSelectedTask,
  clearError,
} from '../../store/taskSlice';
import type { TaskItem } from '../../types';

// Mock API
vi.mock('../../services/api', () => ({
  taskApi: {
    getAllTasks: vi.fn(),
    getTask: vi.fn(),
    createTask: vi.fn(),
    updateTask: vi.fn(),
    deleteTask: vi.fn(),
  },
  tagApi: {
    getAllTags: vi.fn(),
  },
}));

const mockTasks: TaskItem[] = [
  {
    id: 1,
    title: 'Task 1',
    description: 'Description 1',
    dueDate: '2025-12-31T23:59:00',
    priority: 3,
    fullName: 'John Doe',
    telephone: '+1-555-0001',
    email: 'john@example.com',
    tagIds: [1],
    tags: [{ id: 1, name: 'Frontend' }],
    createdAt: '2025-11-28T00:00:00',
    updatedAt: undefined,
  },
];

describe('Task Slice', () => {
  it('should handle initial state', () => {
    const store = configureStore({
      reducer: {
        tasks: taskReducer,
      },
    });

    expect(store.getState().tasks).toEqual({
      tasks: [],
      tags: [],
      selectedTask: null,
      loading: false,
      error: null,
      isCreating: false,
    });
  });

  it('should handle setSelectedTask', () => {
    const store = configureStore({
      reducer: {
        tasks: taskReducer,
      },
    });

    store.dispatch(setSelectedTask(mockTasks[0]));

    expect(store.getState().tasks.selectedTask).toEqual(mockTasks[0]);
  });

  it('should handle clearError', () => {
    const store = configureStore({
      reducer: {
        tasks: taskReducer,
      },
    });
    
    // First set an error by triggering a rejected action
    store.dispatch(loadTasks.rejected(new Error('Test error'), ''));
    expect(store.getState().tasks.error).toBeTruthy();

    // Then clear it
    store.dispatch(clearError());
    expect(store.getState().tasks.error).toBe(null);
  });

  it('should handle loadTasks pending state', () => {
    const store = configureStore({
      reducer: {
        tasks: taskReducer,
      },
    });

    store.dispatch(loadTasks.pending('', undefined));

    expect(store.getState().tasks.loading).toBe(true);
    expect(store.getState().tasks.error).toBe(null);
  });

  it('should handle loadTasks fulfilled state', () => {
    const store = configureStore({
      reducer: {
        tasks: taskReducer,
      },
    });

    store.dispatch(loadTasks.fulfilled(mockTasks, '', undefined));

    expect(store.getState().tasks.tasks).toEqual(mockTasks);
    expect(store.getState().tasks.loading).toBe(false);
  });

  it('should handle loadTasks rejected state', () => {
    const store = configureStore({
      reducer: {
        tasks: taskReducer,
      },
    });

    const error = new Error('Failed to load tasks');
    store.dispatch(loadTasks.rejected(error, '', undefined));

    expect(store.getState().tasks.loading).toBe(false);
    expect(store.getState().tasks.error).toBe('Failed to load tasks');
  });
});
