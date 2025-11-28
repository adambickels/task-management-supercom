import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import TaskForm from '../TaskForm';
import taskSlice from '../../store/taskSlice';
import type { Tag } from '../../types';

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
  { id: 3, name: 'Testing' },
];

const createTestStore = () => {
  return configureStore({
    reducer: {
      tasks: taskSlice,
    },
  });
};

describe('TaskForm Component', () => {
  it('should render form fields when opened', () => {
    const store = createTestStore();
    const mockOnClose = vi.fn();

    render(
      <Provider store={store}>
        <TaskForm open={true} onClose={mockOnClose} task={null} tags={mockTags} />
      </Provider>
    );

    expect(screen.getByLabelText(/title/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/description/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/due date/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/full name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/telephone/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
  });

  it('should have submit button', () => {
    const store = createTestStore();
    const mockOnClose = vi.fn();

    render(
      <Provider store={store}>
        <TaskForm open={true} onClose={mockOnClose} task={null} tags={mockTags} />
      </Provider>
    );

    const submitButton = screen.getByRole('button', { name: /create/i });
    expect(submitButton).toBeInTheDocument();
  });

  it('should populate form when editing existing task', async () => {
    const store = createTestStore();
    const mockOnClose = vi.fn();
    
    const existingTask = {
      id: 1,
      title: 'Existing Task',
      description: 'Task description',
      dueDate: '2025-12-31T23:59:00',
      priority: 4,
      fullName: 'John Doe',
      telephone: '+1-555-0001',
      email: 'john@example.com',
      tagIds: [1, 2],
      tags: mockTags.slice(0, 2),
    createdAt: '2025-11-28T00:00:00',
    updatedAt: undefined,
  };    render(
      <Provider store={store}>
        <TaskForm open={true} onClose={mockOnClose} task={existingTask} tags={mockTags} />
      </Provider>
    );

    await waitFor(() => {
      const titleInput = screen.getByLabelText(/title/i) as HTMLInputElement;
      expect(titleInput.value).toBe('Existing Task');
    });
  });

  it('should call onClose when cancel button is clicked', () => {
    const store = createTestStore();
    const mockOnClose = vi.fn();

    render(
      <Provider store={store}>
        <TaskForm open={true} onClose={mockOnClose} task={null} tags={mockTags} />
      </Provider>
    );

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
  });
});
