import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import TaskList from '../TaskList';
import taskSlice from '../../store/taskSlice';
import type { TaskItem, Tag } from '../../types';

// Mock the API service
vi.mock('../../services/api', () => ({
  taskApi: {
    getTasks: vi.fn(),
    getTask: vi.fn(),
    createTask: vi.fn(),
    updateTask: vi.fn(),
    deleteTask: vi.fn(),
  },
  tagApi: {
    getTags: vi.fn(),
  },
}));

const mockTags: Tag[] = [
  { id: 1, name: 'Frontend' },
  { id: 2, name: 'Backend' },
];

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
    tags: [mockTags[0]],
    createdAt: '2025-11-28T00:00:00',
    updatedAt: undefined,
  },
  {
    id: 2,
    title: 'Task 2',
    description: 'Description 2',
    dueDate: '2025-12-30T23:59:00',
    priority: 5,
    fullName: 'Jane Doe',
    telephone: '+1-555-0002',
    email: 'jane@example.com',
    tagIds: [2],
    tags: [mockTags[1]],
    createdAt: '2025-11-28T00:00:00',
    updatedAt: undefined,
  },
];

const createTestStore = (initialState = {}) => {
  return configureStore({
    reducer: {
      tasks: taskSlice,
    },
    preloadedState: initialState,
  });
};

describe('TaskList Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render tasks when provided', () => {
    const store = createTestStore();
    const mockOnEdit = vi.fn();
    const mockOnDelete = vi.fn();

    render(
      <Provider store={store}>
        <TaskList tasks={mockTasks} onEdit={mockOnEdit} onDelete={mockOnDelete} />
      </Provider>
    );

    expect(screen.getByText('Task 1')).toBeInTheDocument();
    expect(screen.getByText('Task 2')).toBeInTheDocument();
  });

  it('should display task details correctly', () => {
    const store = createTestStore();
    const mockOnEdit = vi.fn();
    const mockOnDelete = vi.fn();

    render(
      <Provider store={store}>
        <TaskList tasks={mockTasks} onEdit={mockOnEdit} onDelete={mockOnDelete} />
      </Provider>
    );

    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('Jane Doe')).toBeInTheDocument();
  });

  it('should display tags for each task', () => {
    const store = createTestStore();
    const mockOnEdit = vi.fn();
    const mockOnDelete = vi.fn();

    render(
      <Provider store={store}>
        <TaskList tasks={mockTasks} onEdit={mockOnEdit} onDelete={mockOnDelete} />
      </Provider>
    );

    expect(screen.getByText('Frontend')).toBeInTheDocument();
    expect(screen.getByText('Backend')).toBeInTheDocument();
  });

  it('should render empty state when no tasks', () => {
    const store = createTestStore();
    const mockOnEdit = vi.fn();
    const mockOnDelete = vi.fn();

    render(
      <Provider store={store}>
        <TaskList tasks={[]} onEdit={mockOnEdit} onDelete={mockOnDelete} />
      </Provider>
    );

    // Component should render without tasks
    expect(screen.queryByText('Task 1')).not.toBeInTheDocument();
  });
});
