import { createSlice, createAsyncThunk, type PayloadAction } from '@reduxjs/toolkit';
import type { TaskItem, CreateTaskRequest, Tag } from '../types';
import { taskApi, tagApi } from '../services/api';

interface TaskState {
  tasks: TaskItem[];
  tags: Tag[];
  selectedTask: TaskItem | null;
  loading: boolean;
  error: string | null;
  isCreating: boolean;
}

const initialState: TaskState = {
  tasks: [],
  tags: [],
  selectedTask: null,
  loading: false,
  error: null,
  isCreating: false,
};

// Async thunks
export const loadTasks = createAsyncThunk('tasks/loadTasks', async () => {
  const tasks = await taskApi.getAllTasks();
  return tasks;
});

export const loadTags = createAsyncThunk('tasks/loadTags', async () => {
  const tags = await tagApi.getAllTags();
  return tags;
});

export const loadTask = createAsyncThunk('tasks/loadTask', async (id: number) => {
  const task = await taskApi.getTask(id);
  return task;
});

export const createTask = createAsyncThunk(
  'tasks/createTask',
  async (task: CreateTaskRequest) => {
    const newTask = await taskApi.createTask(task);
    return newTask;
  },
  {
    condition: (_, { getState }) => {
      const state = getState() as { tasks: TaskState };
      // Don't allow multiple create requests at the same time
      if (state.tasks.isCreating) {
        return false;
      }
      return true;
    },
  }
);

export const updateTask = createAsyncThunk(
  'tasks/updateTask',
  async ({ id, task }: { id: number; task: CreateTaskRequest }) => {
    const updatedTask = await taskApi.updateTask(id, task);
    return updatedTask;
  }
);

export const deleteTask = createAsyncThunk('tasks/deleteTask', async (id: number) => {
  await taskApi.deleteTask(id);
  return id;
});

// Slice
const taskSlice = createSlice({
  name: 'tasks',
  initialState,
  reducers: {
    setSelectedTask: (state, action: PayloadAction<TaskItem | null>) => {
      state.selectedTask = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Load Tasks
    builder
      .addCase(loadTasks.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadTasks.fulfilled, (state, action) => {
        state.tasks = action.payload;
        state.loading = false;
      })
      .addCase(loadTasks.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to load tasks';
      });

    // Load Tags
    builder
      .addCase(loadTags.pending, (state) => {
        state.error = null;
      })
      .addCase(loadTags.fulfilled, (state, action) => {
        state.tags = action.payload;
      })
      .addCase(loadTags.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to load tags';
      });

    // Load Task
    builder
      .addCase(loadTask.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadTask.fulfilled, (state, action) => {
        state.selectedTask = action.payload;
        state.loading = false;
      })
      .addCase(loadTask.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to load task';
      });

    // Create Task
    builder
      .addCase(createTask.pending, (state) => {
        state.loading = true;
        state.error = null;
        state.isCreating = true;
      })
      .addCase(createTask.fulfilled, (state, action) => {
        state.tasks = [action.payload, ...state.tasks];
        state.loading = false;
        state.isCreating = false;
      })
      .addCase(createTask.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to create task';
        state.isCreating = false;
      });

    // Update Task
    builder
      .addCase(updateTask.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(updateTask.fulfilled, (state, action) => {
        const index = state.tasks.findIndex((t) => t.id === action.payload.id);
        if (index !== -1) {
          state.tasks[index] = action.payload;
        }
        state.selectedTask = action.payload;
        state.loading = false;
      })
      .addCase(updateTask.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to update task';
      });

    // Delete Task
    builder
      .addCase(deleteTask.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(deleteTask.fulfilled, (state, action) => {
        state.tasks = state.tasks.filter((t) => t.id !== action.payload);
        state.loading = false;
      })
      .addCase(deleteTask.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to delete task';
      });
  },
});

export const { setSelectedTask, clearError } = taskSlice.actions;
export default taskSlice.reducer;
